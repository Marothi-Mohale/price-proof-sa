namespace PriceProof.Domain.Services;

public interface IRiskScoringEngine
{
    RiskScoreResult Calculate(IEnumerable<RiskCaseSignal> cases, DateTimeOffset now);
}
