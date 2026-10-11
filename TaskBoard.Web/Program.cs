using MudBlazor.Services;
using TaskBoard.Web.Components;
using TaskBoard.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

// Talking to TaskBoard.Api: a named HttpClient (base address from config) plus
// per-circuit services for the signed-in user's token and the typed API wrapper.
builder.Services.AddHttpClient(TaskBoardApiClient.HttpClientName, client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Missing configuration value 'Api:BaseUrl'.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60); // Azure SQL Serverless can take a while to wake up
});
builder.Services.AddScoped<AuthTokenService>();
builder.Services.AddScoped<TaskBoardApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
