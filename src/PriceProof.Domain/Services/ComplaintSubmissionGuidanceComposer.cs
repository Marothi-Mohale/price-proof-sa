using System.Globalization;
using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Services;

public sealed record ComplaintSubmissionGuidanceInput(
    string CaseNumber,
    string MerchantName,
    string? BranchName,
    DateTimeOffset IncidentAtUtc,
    string CurrencyCode,
    decimal QuotedAmount,
    decimal ChargedAmount,
    decimal DifferenceAmount,
    string ClassificationLabel,
    ComplaintEvidenceStrength EvidenceStrength,
    PaymentMethod PaymentMethod);

public sealed record ComplaintSubmissionRoute(
    int Order,
    string Channel,
    string Recipient,
    string Reason,
    string WhenToUse);

public sealed record ComplaintSubmissionEmailTemplate(
    string Subject,
    string Body);

public sealed record ComplaintSubmissionGuidanceResult(
    IReadOnlyCollection<ComplaintSubmissionRoute> RecommendedRoutes,
    string SafeUseNote,
    ComplaintSubmissionEmailTemplate EmailTemplate);

public interface IComplaintSubmissionGuidanceComposer
{
    ComplaintSubmissionGuidanceResult Compose(ComplaintSubmissionGuidanceInput input);
}

public sealed class ComplaintSubmissionGuidanceComposer : IComplaintSubmissionGuidanceComposer
{
    public ComplaintSubmissionGuidanceResult Compose(ComplaintSubmissionGuidanceInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.CaseNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MerchantName);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.CurrencyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ClassificationLabel);

        var routes = BuildRoutes(input);
        var safeUseNote = BuildSafeUseNote(input.EvidenceStrength);
        var emailTemplate = BuildEmailTemplate(input, safeUseNote);

        return new ComplaintSubmissionGuidanceResult(routes, safeUseNote, emailTemplate);
    }

    private static IReadOnlyCollection<ComplaintSubmissionRoute> BuildRoutes(ComplaintSubmissionGuidanceInput input)
    {
        var routes = new List<ComplaintSubmissionRoute>
        {
            new(
                1,
                "Merchant complaint channel",
                string.IsNullOrWhiteSpace(input.BranchName)
                    ? "Store manager or customer care"
                    : $"Branch manager or {input.MerchantName} customer care",
                "Start with the merchant so it can review the price discrepancy and resolve it directly.",
                "Use this first for all cases.")
        };

        var nextOrder = 2;
        var isCardPayment = input.PaymentMethod is PaymentMethod.CreditCard or PaymentMethod.DebitCard;

        if (isCardPayment)
        {
            routes.Add(new ComplaintSubmissionRoute(
                nextOrder++,
                "Bank dispute team",
                "Your bank's card disputes or charge-query team",
                "This case concerns a card payment, so the bank can review the charged transaction amount against the receipt and supporting evidence.",
                "Use this alongside or immediately after the merchant complaint if the disputed amount was charged to a debit or credit card."));
        }

        routes.Add(new ComplaintSubmissionRoute(
            nextOrder++,
            "Consumer Goods and Services Ombud (CGSO)",
            "CGSO complaint intake",
            "CGSO is the supplier-side ombud route when the merchant does not resolve the matter through its own complaints process.",
            "Use this after first giving the merchant a fair opportunity to respond."));

        if (isCardPayment)
        {
            routes.Add(new ComplaintSubmissionRoute(
                nextOrder++,
                "National Financial Ombud Scheme South Africa",
                "Banking complaints intake",
                "If the bank does not resolve the card complaint through its internal process, the banking ombud route is the next escalation point.",
                "Use this only after following the bank's internal complaints or dispute process."));
        }

        routes.Add(new ComplaintSubmissionRoute(
            nextOrder,
            "National Consumer Commission (NCC)",
            "NCC complaints portal",
            "The NCC is the consumer-protection regulator and is appropriate for regulatory escalation where merchant-side routes have not resolved the matter or have referred it onward.",
            "Use this after the supplier complaint path and any relevant ombud steps."));

        return routes;
    }

    private static string BuildSafeUseNote(ComplaintEvidenceStrength evidenceStrength)
    {
        return evidenceStrength switch
        {
            ComplaintEvidenceStrength.Strong =>
                "Use this pack privately with the merchant, your bank, or the relevant complaint channel. Keep the wording factual and avoid posting allegations publicly while the matter is under review.",
            ComplaintEvidenceStrength.Moderate =>
                "Use this pack as a factual complaint record and note where supporting material is incomplete. Keep the complaint private and give the receiving party a fair chance to review the evidence.",
            _ =>
                "Use this pack as a factual starting point only. Say clearly that the evidence is currently limited, avoid overstating the conclusion, and keep the complaint within private resolution channels."
        };
    }

    private static ComplaintSubmissionEmailTemplate BuildEmailTemplate(
        ComplaintSubmissionGuidanceInput input,
        string safeUseNote)
    {
        var merchantLabel = string.IsNullOrWhiteSpace(input.BranchName)
            ? input.MerchantName
            : $"{input.MerchantName} ({input.BranchName})";
        var subject = string.Create(
            CultureInfo.InvariantCulture,
            $"Complaint pack submission: {input.CaseNumber} | {input.MerchantName} | {input.IncidentAtUtc:yyyy-MM-dd}");
        var evidenceStrengthLine = input.EvidenceStrength switch
        {
            ComplaintEvidenceStrength.Strong => "The current evidence record is comparatively strong.",
            ComplaintEvidenceStrength.Moderate => "The current evidence record is moderately complete, although some supporting detail may still be missing.",
            _ => "The current evidence record is limited, and I am disclosing that clearly upfront."
        };

        var body = string.Create(
            CultureInfo.InvariantCulture,
            $"""
            Dear Sir/Madam,

            Please find attached a complaint pack for case reference {input.CaseNumber}, relating to a reported pricing discrepancy at {merchantLabel} on {input.IncidentAtUtc:dd MMM yyyy HH:mm 'UTC'}.

            The recorded quoted or displayed price was {FormatMoney(input.CurrencyCode, input.QuotedAmount)}.
            The recorded charged amount was {FormatMoney(input.CurrencyCode, input.ChargedAmount)}.
            The recorded difference was {FormatMoney(input.CurrencyCode, input.DifferenceAmount)}.

            PriceProof SA currently classifies the discrepancy as {input.ClassificationLabel}. {evidenceStrengthLine}

            I request a factual review of the attached material and a written response setting out the outcome, any adjustment or refund, or the next step in your complaints process.

            {safeUseNote}

            Kind regards,
            [Your full name]
            [Your phone number]
            [Your email address]
            """);

        return new ComplaintSubmissionEmailTemplate(subject, body);
    }

    private static string FormatMoney(string currencyCode, decimal amount)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{currencyCode.ToUpperInvariant()} {amount:0.00}");
    }
}
