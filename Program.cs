using AICodeReviewer.Api.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("GitHub"));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<GitHubService>();
builder.Services.AddSingleton<AnalyzerService>();
builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddSingleton<AiService>();
builder.Services.AddSingleton<DiffSummarizer>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.WriteIndented = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
