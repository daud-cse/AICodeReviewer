using System.Text.RegularExpressions;
using AICodeReviewer.Api.Models;
using AICodeReviewer.Api.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AICodeReviewer.Api.Services;

public class AnalyzerService
{
    public List<RiskItem> Analyze(string filePath, string content)
    {
        var risks = new List<RiskItem>();

        // 1) Basic text heuristics (fast wins)
        FindByRegex(@"async\s+void\s+\w+\s*\(", "R001", "async void usage",
            "Avoid async void except for event handlers; prefer Task-returning methods.",
            Severity: "Warning");
        FindByRegex(@"Thread\.Sleep\s*\(", "R002", "Thread.Sleep in code",
            "Avoid blocking threads. Use await Task.Delay or asynchronous alternatives.",
            "Warning");
        FindByRegex(@"DateTime\.(Now|UtcNow)", "R003", "Non-injectable time source",
            "Use an injectable clock/time provider for deterministic tests.",
            "Info");
        FindByRegex(@"new\s+HttpClient\s*\(", "R004", "HttpClient lifecycle",
            "Use IHttpClientFactory to avoid socket exhaustion.",
            "Info");

        // 2) Roslyn AST checks (catch blocks, swallow exceptions, async/sync mix)
        try
        {
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = tree.GetRoot();

            // async void (AST-confirmation)
            var methodDecls = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var m in methodDecls)
            {
                if (m.Modifiers.Any(SyntaxKind.AsyncKeyword) &&
                    m.ReturnType.ToString() == "void")
                {
                    risks.Add(new RiskItem(filePath, "R001", "async void usage",
                        "Warning", $"Method '{m.Identifier}' is async void.", m.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
                }
            }

            // Swallowing exceptions
            var catches = root.DescendantNodes().OfType<CatchClauseSyntax>();
            foreach (var c in catches)
            {
                var typeStr = c.Declaration?.Type?.ToString() ?? "Exception";
                var hasThrow = c.Block.Statements.Any(s => s is ThrowStatementSyntax);
                var hasLog = c.Block.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Any(i => i.ToString().Contains("log", StringComparison.OrdinalIgnoreCase)
                           || i.ToString().Contains("Logger", StringComparison.OrdinalIgnoreCase));

                if (!hasThrow && !hasLog)
                {
                    risks.Add(new RiskItem(filePath, "R005", "Swallowed exception",
                        "Warning", $"Catches {typeStr} without rethrowing or logging.",
                        c.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
                }
            }

            // .Result / .Wait() inside async methods
            foreach (var m in methodDecls.Where(md => md.Modifiers.Any(SyntaxKind.AsyncKeyword)))
            {
                var hasSyncWait = m.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
                    .Any(ma => ma.Name.ToString() is "Result" or "Wait");
                if (hasSyncWait)
                {
                    risks.Add(new RiskItem(filePath, "R006", "Sync over async",
                        "Warning", $"Method '{m.Identifier}' blocks on async calls (.Result/.Wait).",
                        m.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
                }
            }
        }
        catch
        {
            // ignore parsing errors; file might be non-C#
        }

        return risks;

        void FindByRegex(string pattern, string rule, string title, string message, string Severity)
        {
            var rx = new Regex(pattern, RegexOptions.Compiled);
            foreach (Match m in rx.Matches(content))
            {
                // Line calc (best-effort)
                var line = content[..m.Index].Count(c => c == '\n') + 1;
                risks.Add(new RiskItem(filePath, rule, title, Severity, message, line));
            }
        }
    }
}
