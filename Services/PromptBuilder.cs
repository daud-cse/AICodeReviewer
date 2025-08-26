using System.Text;
using AICodeReviewer.Api.Models;


namespace AICodeReviewer.Api.Services;

public class PromptBuilder
{
    public string BuildReviewPrompt(
        string prTitle,
        string prUrl,
        IEnumerable<FileChange> files,
        IEnumerable<RiskItem> risks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a senior .NET code reviewer.");
        sb.AppendLine("Task: Summarize risky changes in this PR and propose concrete unit test cases (xUnit).");
        sb.AppendLine("Respond in concise Markdown. Use bullet points. Include file paths where relevant.");
        sb.AppendLine();
        sb.AppendLine($"PR: {prTitle}");
        sb.AppendLine($"URL: {prUrl}");
        sb.AppendLine();

        sb.AppendLine("## Changes");
        foreach (var f in files)
        {
            sb.AppendLine($"- `{f.FilePath}` ({f.Status}) +{f.Additions}/-{f.Deletions}");
        }
        sb.AppendLine();

        sb.AppendLine("## Detected Risks (static analysis)");
        if (!risks.Any()) sb.AppendLine("- No obvious risks found by heuristics.");
        foreach (var r in risks)
        {
            sb.AppendLine($"- [{r.Severity}] `{r.FilePath}`:{r.Line} **{r.Title} ({r.RuleId})** – {r.Message}");
        }
        sb.AppendLine();

        sb.AppendLine("## Output format");
        sb.AppendLine("Return a JSON object with:");
        sb.AppendLine("  risks: [{filePath, ruleId, title, severity, message, line?}],");
        sb.AppendLine("  tests: [{title, rationale, exampleXunit}],");
        sb.AppendLine("  summaryMarkdown: string");
        sb.AppendLine();

        sb.AppendLine("## Additional guidance");
        sb.AppendLine("- Prefer precise, actionable tests tied to changed methods and edge cases.");
        sb.AppendLine("- If risks are missing, infer likely ones from context (e.g., null/arg validation, DI usage).");
        return sb.ToString();
    }
}
