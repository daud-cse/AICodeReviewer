# AI Code Reviewer

An intelligent .NET Web API that automatically reviews GitHub pull requests using AI-powered analysis and static code analysis. The system combines OpenAI's language models with Roslyn-based C# code analysis to provide comprehensive code review feedback.

## 🚀 Features

- **AI-Powered Code Review**: Uses OpenAI GPT models to analyze code changes and provide intelligent feedback
- **Static Code Analysis**: Built-in Roslyn-based analyzer for C# code that detects common anti-patterns and risks
- **GitHub Integration**: Seamlessly integrates with GitHub pull requests via the GitHub API
- **Automated Commenting**: Can automatically post review results as comments on pull requests
- **Risk Assessment**: Identifies potential issues with severity levels (Info, Warning, Error)
- **Test Suggestions**: Provides AI-generated test recommendations based on code changes
- **Comprehensive Reporting**: Generates detailed markdown summaries of code reviews

## 🏗️ Architecture

The project follows a clean service-oriented architecture with the following components:

### Core Services

- **`GitHubService`**: Handles GitHub API interactions for pull requests and file content
- **`AiService`**: Manages OpenAI API communication for AI-powered analysis
- **`AnalyzerService`**: Performs static code analysis using Roslyn
- **`PromptBuilder`**: Constructs optimized prompts for AI analysis
- **`DiffSummarizer`**: Analyzes and summarizes code changes

### Data Models

- **`ReviewRequest`**: API request parameters
- **`ReviewResult`**: Complete review analysis results
- **`RiskItem`**: Individual risk/issue identified during analysis
- **`SuggestedTest`**: AI-generated test recommendations
- **`FileChange`**: Represents changes in individual files

## 🛠️ Technology Stack

- **.NET 8.0**: Modern, high-performance web framework
- **ASP.NET Core**: Web API framework with dependency injection
- **Roslyn**: Microsoft's .NET compiler platform for code analysis
- **Octokit**: GitHub API client library
- **OpenAI API**: GPT models for intelligent code analysis
- **Swagger/OpenAPI**: API documentation and testing interface

## 📋 Prerequisites

- .NET 8.0 SDK or later
- GitHub account with repository access
- OpenAI API key (optional, but recommended for full functionality)

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone <your-repository-url>
cd AICodeReviewer
```

### 2. Configure Settings

Create or update `appsettings.json` with your configuration:

```json
{
  "GitHub": {
    "Token": "your-github-personal-access-token",
    "UserAgent": "AICodeReviewer/1.0"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4" // or "gpt-3.5-turbo"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 3. Run the Application

```bash
dotnet restore
dotnet run
```

The API will be available at `https://localhost:5001` (or the configured port).

### 4. Access Swagger UI

Navigate to `https://localhost:5001/swagger` to explore the API endpoints interactively.

## 📖 API Usage

### Review a Pull Request

```http
POST /api/review/{owner}/{repo}/{number}
Content-Type: application/json

{
  "postComment": true
}
```

**Parameters:**
- `owner`: GitHub repository owner/organization
- `repo`: Repository name
- `number`: Pull request number
- `postComment`: Whether to automatically post the review as a comment

**Response:**

```json
{
  "prTitle": "Add new feature",
  "prUrl": "https://github.com/owner/repo/pull/123",
  "filesChanged": 5,
  "additions": 150,
  "deletions": 25,
  "risks": [
    {
      "filePath": "Services/MyService.cs",
      "ruleId": "R001",
      "title": "async void usage",
      "severity": "Warning",
      "message": "Avoid async void except for event handlers",
      "line": 42
    }
  ],
  "suggestedTests": [
    {
      "title": "Test async method behavior",
      "rationale": "Ensure proper error handling",
      "exampleXunit": "[Fact]\npublic async Task Should_Handle_Async_Operation()\n{\n  // Test implementation\n}"
    }
  ],
  "aiSummaryMarkdown": "## Code Review Summary\n\n..."
}
```

## 🔍 Static Analysis Rules

The built-in analyzer detects several common C# anti-patterns:

| Rule ID | Title | Severity | Description |
|----------|-------|----------|-------------|
| R001 | async void usage | Warning | Avoid async void except for event handlers |
| R002 | Thread.Sleep in code | Warning | Avoid blocking threads, use Task.Delay |
| R003 | Non-injectable time source | Info | Use injectable clock for testability |
| R004 | HttpClient lifecycle | Info | Use IHttpClientFactory to avoid socket exhaustion |
| R005 | Swallowed exception | Warning | Exceptions should be logged or rethrown |
| R006 | Sync over async | Warning | Avoid .Result/.Wait in async methods |

## 🔧 Configuration Options

### GitHub Configuration

- **Token**: Personal access token with repo access
- **UserAgent**: Custom user agent for API requests

### OpenAI Configuration

- **ApiKey**: Your OpenAI API key
- **Model**: GPT model to use (gpt-4, gpt-3.5-turbo, etc.)

## 🧪 Testing

The project includes comprehensive test coverage for all services. Run tests with:

```bash
dotnet test
```

## 📦 Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AICodeReviewer.csproj", "./"]
RUN dotnet restore "AICodeReviewer.csproj"
COPY . .
RUN dotnet build "AICodeReviewer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AICodeReviewer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AICodeReviewer.dll"]
```

### Azure App Service

The project is ready for deployment to Azure App Service with minimal configuration changes.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Microsoft for .NET and Roslyn
- OpenAI for GPT models
- GitHub for their excellent API
- The .NET community for inspiration and best practices

## 📞 Support

For questions, issues, or contributions, please:

1. Check existing issues in the repository
2. Create a new issue with detailed information
3. Contact the maintainers directly

---

**Note**: This project requires an OpenAI API key for full functionality. Without it, the system will fall back to static analysis only.
