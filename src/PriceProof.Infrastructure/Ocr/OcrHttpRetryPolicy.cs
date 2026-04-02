using System.Net;
using Microsoft.Extensions.Logging;

namespace PriceProof.Infrastructure.Ocr;

internal static class OcrHttpRetryPolicy
{
    public static async Task<HttpResponseMessage> ExecuteAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> operation,
        int retryCount,
        int delayMilliseconds,
        string providerName,
        string operationName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, retryCount + 1);
        var delay = Math.Max(100, delayMilliseconds);
        HttpResponseMessage? lastResponse = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                var response = await operation(cancellationToken);
                if (!IsTransientStatus(response.StatusCode) || attempt == attempts)
                {
                    return response;
                }

                lastResponse?.Dispose();
                lastResponse = response;
                logger.LogWarning(
                    "Transient OCR response from {Provider} during {Operation} attempt {Attempt} with status {StatusCode}. Retrying.",
                    providerName,
                    operationName,
                    attempt,
                    (int)response.StatusCode);
            }
            catch (HttpRequestException exception) when (attempt < attempts)
            {
                logger.LogWarning(
                    exception,
                    "Transient OCR transport failure from {Provider} during {Operation} attempt {Attempt}. Retrying.",
                    providerName,
                    operationName,
                    attempt);
            }

            if (attempt < attempts)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        return lastResponse ?? throw new HttpRequestException($"OCR request to provider '{providerName}' failed after all retry attempts.");
    }

    public static bool IsTransientStatus(HttpStatusCode statusCode)
    {
        var status = (int)statusCode;
        return status == 408 || status == 429 || status >= 500;
    }
}
