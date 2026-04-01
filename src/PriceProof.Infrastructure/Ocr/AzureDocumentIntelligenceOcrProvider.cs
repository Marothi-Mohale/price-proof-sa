using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Infrastructure.Options;

namespace PriceProof.Infrastructure.Ocr;

public sealed class AzureDocumentIntelligenceOcrProvider : IOcrProvider
{
    private readonly HttpClient _httpClient;
    private readonly OcrOptions.AzureDocumentIntelligenceOptions _options;

    public AzureDocumentIntelligenceOcrProvider(HttpClient httpClient, IOptions<OcrOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.AzureDocumentIntelligence;
    }

    public string Name => "AzureDocumentIntelligence";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Endpoint) &&
        !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<OcrProviderResult> RecognizeAsync(OcrDocumentContent document, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return Failure("Azure Document Intelligence is not configured.", false);
        }

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.Endpoint!.TrimEnd('/')}/documentintelligence/documentModels/{_options.ModelId}:analyze?api-version={_options.ApiVersion}");

        message.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
        message.Content = new ByteArrayContent(document.Content);
        message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        if (response.StatusCode is not HttpStatusCode.Accepted and not HttpStatusCode.OK)
        {
            return Failure($"Azure Document Intelligence request failed with status {(int)response.StatusCode}.", IsTransientStatus(response.StatusCode));
        }

        var operationLocation = response.Headers.TryGetValues("operation-location", out var values)
            ? values.FirstOrDefault()
            : response.Headers.TryGetValues("Operation-Location", out var alternateValues)
                ? alternateValues.FirstOrDefault()
                : null;

        if (string.IsNullOrWhiteSpace(operationLocation))
        {
            return Failure("Azure Document Intelligence did not return an operation location.", true);
        }

        for (var attempt = 0; attempt < Math.Max(1, _options.MaxPollingAttempts); attempt++)
        {
            await Task.Delay(Math.Max(250, _options.PollingIntervalMilliseconds), cancellationToken);

            using var pollResponse = await _httpClient.GetAsync(operationLocation, cancellationToken);
            if (!pollResponse.IsSuccessStatusCode)
            {
                if (attempt == _options.MaxPollingAttempts - 1)
                {
                    return Failure($"Azure Document Intelligence polling failed with status {(int)pollResponse.StatusCode}.", IsTransientStatus(pollResponse.StatusCode));
                }

                continue;
            }

            await using var stream = await pollResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var documentJson = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = documentJson.RootElement;
            var status = GetString(root, "status");

            if (status?.Equals("succeeded", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Success(root, operationLocation);
            }

            if (status?.Equals("failed", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Failure("Azure Document Intelligence OCR failed.", false);
            }
        }

        return Failure("Azure Document Intelligence timed out while waiting for analysis completion.", true);
    }

    private OcrProviderResult Success(JsonElement root, string operationLocation)
    {
        var analyzeResult = root.TryGetProperty("analyzeResult", out var analyzeResultElement)
            ? analyzeResultElement
            : default;

        var rawText = GetString(analyzeResult, "content") ?? string.Empty;
        var fields = GetReceiptFields(analyzeResult);

        var merchantName = GetStringField(fields, "MerchantName");
        var transactionTotal = GetCurrencyAmountField(fields, "Total") ?? GetNumberField(fields, "Total");
        var transactionAtUtc = ParseTransactionAt(fields);
        var lineItems = GetLineItems(fields);
        var confidence = Average(
            GetFieldConfidence(fields, "MerchantName"),
            GetFieldConfidence(fields, "Total"),
            GetFieldConfidence(fields, "TransactionDate"),
            GetFieldConfidence(fields, "TransactionTime"));

        var metadataJson = JsonSerializer.Serialize(new
        {
            provider = Name,
            status = GetString(root, "status"),
            modelId = _options.ModelId,
            apiVersion = _options.ApiVersion,
            operationLocation,
            pageCount = analyzeResult.ValueKind == JsonValueKind.Object && analyzeResult.TryGetProperty("pages", out var pages)
                ? pages.GetArrayLength()
                : 0,
            documentCount = analyzeResult.ValueKind == JsonValueKind.Object && analyzeResult.TryGetProperty("documents", out var documents)
                ? documents.GetArrayLength()
                : 0
        });

        return new OcrProviderResult(
            Name,
            !string.IsNullOrWhiteSpace(rawText) || merchantName is not null || transactionTotal.HasValue,
            rawText,
            metadataJson,
            confidence,
            merchantName,
            transactionTotal,
            transactionAtUtc,
            lineItems);
    }

    private static JsonElement GetReceiptFields(JsonElement analyzeResult)
    {
        if (analyzeResult.ValueKind == JsonValueKind.Object &&
            analyzeResult.TryGetProperty("documents", out var documents) &&
            documents.ValueKind == JsonValueKind.Array &&
            documents.GetArrayLength() > 0 &&
            documents[0].TryGetProperty("fields", out var fields))
        {
            return fields;
        }

        return default;
    }

    private static IReadOnlyCollection<OcrLineItem> GetLineItems(JsonElement fields)
    {
        if (fields.ValueKind != JsonValueKind.Object ||
            !fields.TryGetProperty("Items", out var itemsField) ||
            !itemsField.TryGetProperty("valueArray", out var valueArray) ||
            valueArray.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<OcrLineItem>();
        }

        var items = new List<OcrLineItem>();

        foreach (var item in valueArray.EnumerateArray())
        {
            if (!item.TryGetProperty("valueObject", out var valueObject))
            {
                continue;
            }

            var description = GetStringFromObjectField(valueObject, "Description") ?? GetStringFromObjectField(valueObject, "Name");
            var quantity = GetNumberFromObjectField(valueObject, "Quantity");
            var totalAmount = GetCurrencyFromObjectField(valueObject, "TotalPrice") ?? GetCurrencyFromObjectField(valueObject, "Amount");
            var unitPrice = GetCurrencyFromObjectField(valueObject, "Price");

            if (string.IsNullOrWhiteSpace(description) && !totalAmount.HasValue)
            {
                continue;
            }

            items.Add(new OcrLineItem(description ?? "Line item", totalAmount, quantity, unitPrice));
        }

        return items;
    }

    private static DateTimeOffset? ParseTransactionAt(JsonElement fields)
    {
        var date = GetDateField(fields, "TransactionDate");
        var time = GetTimeField(fields, "TransactionTime");

        if (!date.HasValue)
        {
            return null;
        }

        return time.HasValue
            ? new DateTimeOffset(date.Value.Add(time.Value), TimeSpan.Zero)
            : new DateTimeOffset(date.Value, TimeSpan.Zero);
    }

    private static decimal? GetFieldConfidence(JsonElement fields, string fieldName)
    {
        return fields.ValueKind == JsonValueKind.Object &&
               fields.TryGetProperty(fieldName, out var field) &&
               field.TryGetProperty("confidence", out var confidence) &&
               confidence.ValueKind == JsonValueKind.Number
            ? confidence.GetDecimal()
            : null;
    }

    private static string? GetStringField(JsonElement fields, string fieldName)
    {
        if (fields.ValueKind != JsonValueKind.Object || !fields.TryGetProperty(fieldName, out var field))
        {
            return null;
        }

        return GetString(field, "valueString") ?? GetString(field, "content");
    }

    private static decimal? GetCurrencyAmountField(JsonElement fields, string fieldName)
    {
        if (fields.ValueKind != JsonValueKind.Object ||
            !fields.TryGetProperty(fieldName, out var field) ||
            !field.TryGetProperty("valueCurrency", out var currency) ||
            !currency.TryGetProperty("amount", out var amount))
        {
            return null;
        }

        return amount.ValueKind switch
        {
            JsonValueKind.Number => amount.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(amount.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static decimal? GetNumberField(JsonElement fields, string fieldName)
    {
        if (fields.ValueKind != JsonValueKind.Object ||
            !fields.TryGetProperty(fieldName, out var field) ||
            !field.TryGetProperty("valueNumber", out var number) ||
            number.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return number.GetDecimal();
    }

    private static DateTime? GetDateField(JsonElement fields, string fieldName)
    {
        var value = fields.ValueKind == JsonValueKind.Object &&
                    fields.TryGetProperty(fieldName, out var field)
            ? GetString(field, "valueDate") ?? GetString(field, "content")
            : null;

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null;
    }

    private static TimeSpan? GetTimeField(JsonElement fields, string fieldName)
    {
        var value = fields.ValueKind == JsonValueKind.Object &&
                    fields.TryGetProperty(fieldName, out var field)
            ? GetString(field, "valueTime") ?? GetString(field, "content")
            : null;

        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string? GetStringFromObjectField(JsonElement objectField, string fieldName)
    {
        return objectField.TryGetProperty(fieldName, out var field)
            ? GetString(field, "valueString") ?? GetString(field, "content")
            : null;
    }

    private static decimal? GetCurrencyFromObjectField(JsonElement objectField, string fieldName)
    {
        return objectField.TryGetProperty(fieldName, out var field)
            ? GetCurrencyAmountField(CreateSingleFieldObject(fieldName, field), fieldName)
            : null;
    }

    private static decimal? GetNumberFromObjectField(JsonElement objectField, string fieldName)
    {
        return objectField.TryGetProperty(fieldName, out var field)
            ? GetNumberField(CreateSingleFieldObject(fieldName, field), fieldName)
            : null;
    }

    private static JsonElement CreateSingleFieldObject(string propertyName, JsonElement propertyValue)
    {
        using var document = JsonDocument.Parse($"{{\"{propertyName}\":{propertyValue.GetRawText()}}}");
        return document.RootElement.Clone();
    }

    private static decimal? Average(params decimal?[] values)
    {
        var available = values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        return available.Length == 0 ? null : decimal.Round(available.Average(), 4, MidpointRounding.AwayFromZero);
    }

    private static bool IsTransientStatus(HttpStatusCode statusCode)
    {
        var status = (int)statusCode;
        return status == 408 || status == 429 || status >= 500;
    }

    private static OcrProviderResult Failure(string message, bool isTransientFailure)
    {
        return new OcrProviderResult(
            "AzureDocumentIntelligence",
            false,
            string.Empty,
            "{}",
            null,
            null,
            null,
            null,
            null,
            null,
            message,
            isTransientFailure);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }
}
