using backend.Dtos;

namespace backend.Services;

public interface IOpenRouterService
{
    Task<ChatAnalysisDto> AnalyzeIssueAsync(string message, CancellationToken cancellationToken);
}
