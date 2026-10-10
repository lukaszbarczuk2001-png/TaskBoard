using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Core.Entities;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<TaskBoardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TaskBoardDb")));

// Identity + built-in token endpoints (register/login/refresh) under /api/auth/*
builder.Services
    .AddIdentityApiEndpoints<AppUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<TaskBoardDbContext>();

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth").MapIdentityApi<AppUser>();

var projects = app.MapGroup("/api/projects")
    .RequireAuthorization(policy => policy
        .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
        .RequireAuthenticatedUser());

projects.MapGet("/", async (TaskBoardDbContext db) =>
    await db.Projects.AsNoTracking().ToListAsync());

projects.MapGet("/{id:guid}", async (Guid id, TaskBoardDbContext db) =>
    await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
        is { } project
        ? Results.Ok(project)
        : Results.NotFound());

projects.MapPost("/", async (Project project, TaskBoardDbContext db) =>
{
    db.Projects.Add(project);
    await db.SaveChangesAsync();
    return Results.Created($"/api/projects/{project.Id}", project);
});

app.Run();
