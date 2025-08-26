using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AICodeReviewer.Api.Models;
using AICodeReviewer.Api.Services;
using Microsoft.Extensions.Options;

namespace AICodeReviewer.Api.Services;

public class AiService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly OpenAiOptions _opts;

    public AiService(IHttpClientFactory httpFactory, IOptions<OpenAiOptions> opts)
    {
        _httpFactory = httpFactory;
        _opts = opts.Value;
    }

    public async Task<(List<RiskItem> risks, List<SuggestedTest> tests, string summary)>
        SummarizeAsync(string prompt, IEnumerable<RiskItem> staticRisks)
    {
        // If no API key, produce a deterministic fallback using the static risks.
        if (string.IsNullOrWhiteSpace(_opts.ApiKey))
        {
            var summary = new StringBuilder();
            summary.AppendLine("### Summary (fallback)");
            summary.AppendLine("- OpenAI API key not configured; showing static analysis results only.");
            if (!staticRisks.Any()) summary.AppendLine("- No major risks detected by heuristics.");
            else
            {
                summary.AppendLine("- Risks detected:");
                foreach (var r in staticRisks) summary.AppendLine($"  - {r.Title} in `{r.FilePath}`:{r.Line} ({r.Severity})");
            }

            // naive test ideas
            var tests = staticRisks.Select(r => new SuggestedTest(
                $"Covers {r.Title} ({r.RuleId})",
                $"Guard against {r.Message}",
                """
                [Fact]
                public async Task Should_Handle_Risk_Scenario()
                {
                    // Arrange

                    // Act

                    // Assert
                }
                """
            )).ToList();

            return (staticRisks.ToList(), tests, summary.ToString());
        }

        // OpenAI Chat Completions (simple, robust)
        var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.openai.com/v1/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);

        var req = new
        {
            model = _opts.Model,
            messages = new object[]
            {
                new { role = "system", content = "You are a precise senior .NET code reviewer." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json")
        };

        using var resp = await client.SendAsync(httpReq);
        resp.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var content = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        // Try to parse the expected JSON block; if it isn't pure JSON, attempt to extract it.
        var (aiRisks, aiTests, aiSummary) = TryParse(content, staticRisks);
        return (aiRisks, aiTests, aiSummary);

        static (List<RiskItem>, List<SuggestedTest>, string) TryParse(
            string? content,
            IEnumerable<RiskItem> fallbackRisks)
        {
            if (string.IsNullOrWhiteSpace(content))
                return (fallbackRisks.ToList(), new(), "Empty AI response.");

            // crude JSON extraction
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                var jsonSlice = content[start..(end + 1)];
                try
                {
                    using var doc = JsonDocument.Parse(jsonSlice);
                    var root = doc.RootElement;

                    var risks = new List<RiskItem>();
                    if (root.TryGetProperty("risks", out var risksArr) && risksArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in risksArr.EnumerateArray())
                        {
                            risks.Add(new RiskItem(
                                r.GetPropertyOrDefault("filePath", ""),
                                r.GetPropertyOrDefault("ruleId", "AI"),
                                r.GetPropertyOrDefault("title", "Risk"),
                                r.GetPropertyOrDefault("severity", "Info"),
                                r.GetPropertyOrDefault("message", ""),
                                r.TryGetProperty("line", out var l) && l.ValueKind == JsonValueKind.Number ? l.GetInt32() : (int?)null
                            ));
                        }
                    }

                    var tests = new List<SuggestedTest>();
                    if (root.TryGetProperty("tests", out var testArr) && testArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var t in testArr.EnumerateArray())
                        {
                            tests.Add(new SuggestedTest(
                                t.GetPropertyOrDefault("title", "Test"),
                                t.GetPropertyOrDefault("rationale", ""),
                                t.GetPropertyOrDefault("exampleXunit", "")
                            ));
                        }
                    }

                    var summary = root.GetPropertyOrDefault("summaryMarkdown", content);
                    return (risks.Count > 0 ? risks : fallbackRisks.ToList(), tests, summary);
                }
                catch
                {
                    // fall back
                }
            }

            return (fallbackRisks.ToList(), new(), content);
        }
    }
}

static class JsonExt
{
    public static string GetPropertyOrDefault(this JsonElement el, string name, string defaultVal)
        => el.TryGetProperty(name, out var v) && v.ValueKind is not JsonValueKind.Null
            ? v.ToString() ?? defaultVal
            : defaultVal;
}
