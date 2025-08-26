namespace AICodeReviewer.Api.Models;

public record ReviewRequest(bool PostComment = false);

public record ReviewResult(
    string PrTitle,
    string PrUrl,
    int FilesChanged,
    int Additions,
    int Deletions,
    List<RiskItem> Risks,
    List<SuggestedTest> SuggestedTests,
    string AiSummaryMarkdown
);

public record RiskItem(
    string FilePath,
    string RuleId,
    string Title,
    string Severity, // Info | Warning | Error
    string Message,
    int? Line = null
);

public record SuggestedTest(
    string Title,
    string Rationale,
    string ExampleXunit
);

public record FileChange(
    string FilePath,
    string Status, // added | modified | removed | renamed
    int Additions,
    int Deletions,
    string? Patch,   // unified diff hunk from GitHub
    string? NewContent // content at head SHA
);
