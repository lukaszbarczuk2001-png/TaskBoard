using System.Text.Json.Serialization;
using TaskBoard.Api.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Database
// EnableRetryOnFailure: Azure SQL Serverless auto-pauses when idle, so the first
// connection after a pause can fail while the database resumes. Retry transient errors.
builder.Services.AddDbContext<TaskBoardDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TaskBoardDb"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(15), errorNumbersToAdd: null)));

// Identity + built-in token endpoints (register/login/refresh) under /api/auth/*
builder.Services
    .AddIdentityApiEndpoints<AppUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<TaskBoardDbContext>();

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

// Serialize enums (IssueStatus, IssuePriority, IssueType) as their names
// ("InProgress") instead of raw ints in both requests and responses.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth").MapIdentityApi<AppUser>();

app.MapProjectEndpoints();
app.MapIssueEndpoints();

app.Run();
