using AICodeReviewer.Api.Services;
using AICodeReviewer.Api.Models; // fixed namespace casing
using Microsoft.AspNetCore.Mvc;
using Octokit;
using AICodeReviewer.Api.Models;

namespace AiCodeReviewer.Api.Controllers;

[ApiController]
[Route("api/review")]
public class ReviewController : ControllerBase
{
    private readonly GitHubService _gh;
    private readonly AnalyzerService _analyzer;
    private readonly PromptBuilder _prompts;
    private readonly AiService _ai;
    private readonly DiffSummarizer _diff;

    public ReviewController(
        GitHubService gh,
        AnalyzerService analyzer,
        PromptBuilder prompts,
        AiService ai,
        DiffSummarizer diff)
    {
        _gh = gh;
        _analyzer = analyzer;
        _prompts = prompts;
        _ai = ai;
        _diff = diff;
    }

    // POST /api/review/{owner}/{repo}/{number}
    [HttpPost("{owner}/{repo}/{number:int}")]
    public async Task<ActionResult<ReviewResult>> Review(
        string owner,
        string repo,
        int number,
        [FromBody] ReviewRequest request)
    {
        var (pr, files, head) = await _gh.GetPullRequestAsync(owner, repo, number);

        // Pull file content and map
        var changes = new List<FileChange>();
        foreach (var f in files)
        {
            // status is a string: "added", "removed", "modified", "renamed"
            var status = f.Status?.ToLowerInvariant() ?? "modified";

            var content = status == "removed"
                ? null
                : await _gh.GetFileContentAtRefAsync(owner, repo, f.FileName, head);

            changes.Add(new FileChange(
                FilePath: f.FileName,
                Status: status,
                Additions: f.Additions, //?? 0,
                Deletions: f.Deletions, //?? 0,
                Patch: f.Patch,
                NewContent: content
            ));
        }

        // Run static analysis on C# files only
        var risks = new List<RiskItem>();
        foreach (var c in changes.Where(c =>
                     c.NewContent != null &&
                     c.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            risks.AddRange(_analyzer.Analyze(c.FilePath, c.NewContent!));
        }

        var prompt = _prompts.BuildReviewPrompt(pr.Title, pr.HtmlUrl, changes, risks);
        var (aiRisks, tests, summary) = await _ai.SummarizeAsync(prompt, risks);

        var (fc, adds, dels) = _diff.Tally(changes);

        var result = new ReviewResult(
            PrTitle: pr.Title,
            PrUrl: pr.HtmlUrl,
            FilesChanged: fc,
            Additions: adds,
            Deletions: dels,
            Risks: aiRisks,
            SuggestedTests: tests,
            AiSummaryMarkdown: summary
        );

        if (request.PostComment)
        {
            var md = $"## AI Code Review\n\n{summary}\n\n### Risks\n" +
                     string.Join("\n", result.Risks.Select(r =>
                         $"- **{r.Severity}** `{r.FilePath}`:{r.Line} — **{r.Title}** ({r.RuleId}): {r.Message}")) +
                     "\n\n### Suggested Tests\n" +
                     string.Join("\n\n", result.SuggestedTests.Select(t =>
                         $"- **{t.Title}**\n  - {t.Rationale}\n  ```csharp\n{t.ExampleXunit}\n```"));

            await _gh.PostPrCommentAsync(owner, repo, number, md);
        }

        return Ok(result);
    }
}
