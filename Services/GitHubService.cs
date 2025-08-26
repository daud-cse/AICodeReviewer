using AICodeReviewer.Api.Models;
using AICodeReviewer.Api.Services;
using Microsoft.Extensions.Options;
using Octokit;

namespace AICodeReviewer.Api.Services;

public class GitHubService
{
    private readonly GitHubClient _client;

    public GitHubService(IOptions<GitHubOptions> options)
    {
        var product = new ProductHeaderValue("ai-code-reviewer");
        _client = new GitHubClient(product);
        var token = options.Value.Token ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
        {
            _client.Credentials = new Credentials(token);
        }
    }

    public async Task<(PullRequest pr, IReadOnlyList<PullRequestFile> files, string headSha)>
        GetPullRequestAsync(string owner, string repo, int number)
    {
        var pr = await _client.PullRequest.Get(owner, repo, number);
        var files = await _client.PullRequest.Files(owner, repo, number);
        var headSha = pr.Head?.Sha ?? throw new InvalidOperationException("Missing head SHA.");
        return (pr, files, headSha);
    }

    public async Task<string?> GetFileContentAtRefAsync(string owner, string repo, string path, string @ref)
    {
        try
        {
            var contents = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, @ref);

            return contents.FirstOrDefault().Content;
            //return contents.FirstOrDefault()?.Content is string b64
            //    ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64))
            //    : null;
        }
        catch (NotFoundException)
        {
            return null; // removed/renamed files
        }
    }

    public async Task PostPrCommentAsync(string owner, string repo, int number, string markdown)
    {
        await _client.Issue.Comment.Create(owner, repo, number, markdown);
    }
}
