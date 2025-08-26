namespace AICodeReviewer.Api.Services;

public class GitHubOptions
{
    public string? Token { get; set; }
}

public class OpenAiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
}
