namespace PriceProof.Domain.Services;

public interface IDiscrepancyDetectionEngine
{
    DiscrepancyAnalysisResult Analyze(DiscrepancyAnalysisInput input);
}

public interface IDiscrepancyAnalysisRule
{
    bool TryEvaluate(DiscrepancyDetectionEngine.DiscrepancyAnalysisContext context, out DiscrepancyAnalysisResult result);
}
