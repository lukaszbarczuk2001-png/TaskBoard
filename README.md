# TaskBoard

A Jira-style issue tracker built from scratch as a learning project: **Blazor** front end, **ASP.NET Core** minimal API and **Azure SQL** for storage.

It has projects, a Kanban board with drag-and-drop, issue details (edit, delete, comments), assignees with avatars, search and filters, a per-project dashboard, and a light/dark theme with a Linear-inspired look.

## Tech stack

| Layer | Technology |
|-------|------------|
| UI | Blazor Web App (Interactive Server), [MudBlazor](https://mudblazor.com) 9, custom Inter/indigo theme |
| API | ASP.NET Core minimal API (.NET 10), ASP.NET Core Identity with bearer tokens |
| Data | EF Core 10 + Azure SQL (SQL Server provider), code-first migrations |
| Tests | xUnit |

## Solution layout

```
TaskBoard.Core            Entities (Project, Issue, Comment) and enums, no dependencies
TaskBoard.Infrastructure  EF Core DbContext, Identity user, migrations
TaskBoard.Api             Minimal API: auth, projects, issues, comments, users
TaskBoard.Web             Blazor UI that talks to the API over HTTP
TaskBoard.Tests           xUnit tests
```

The Web project never touches the database: it calls the API with a bearer token. DTOs live next to each side, and the API returns plain status codes that the Web client wraps in an `ApiResult<T>` instead of throwing.

## Running it locally

Prerequisites: .NET 10 SDK, and a SQL Server or Azure SQL database you can connect to (Azure SQL serverless is cheap for experiments; remember it auto-pauses, so the first request after a pause can be slow).

1. **Connection string.** Never commit it. Store it in User Secrets of the API project:

   ```bash
   cd TaskBoard.Api
   dotnet user-secrets set "ConnectionStrings:TaskBoardDb" "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=TaskBoardDb;User ID=<user>;Password=<password>;Encrypt=True;"
   ```

2. **Create the schema** from the migrations:

   ```bash
   dotnet ef database update --project TaskBoard.Infrastructure --startup-project TaskBoard.Api
   ```

3. **Start both projects** (API on `https://localhost:7280`, Web on `https://localhost:7094`). In Visual Studio use *Configure Startup Projects → Multiple startup projects* and set `TaskBoard.Api` and `TaskBoard.Web` to *Start*. From the CLI, run each in its own terminal:

   ```bash
   dotnet run --project TaskBoard.Api --launch-profile https
   dotnet run --project TaskBoard.Web --launch-profile https
   ```

4. Open `https://localhost:7094`, choose **Create account**, then create a project and your first issues. Passwords need 6+ characters with upper and lower case, a digit and a symbol.

The API address the UI calls is `Api:BaseUrl` in `TaskBoard.Web/appsettings.json`.

## Notes and known limitations

- The access token is kept only in the Blazor circuit's memory, so reloading the page asks you to sign in again.
- Every signed-in user sees every project (there is no project membership yet).
- Test coverage is minimal; the focus of this project was the architecture and the end-to-end flow.

## Possible next steps

Persistent sign-in, project membership and roles, richer issue cards, API integration tests with `WebApplicationFactory`, and a CI workflow.
