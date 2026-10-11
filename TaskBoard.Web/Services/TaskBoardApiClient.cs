using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskBoard.Core.Enums;

namespace TaskBoard.Web.Services;

/// <summary>
/// Typed wrapper around TaskBoard.Api. Scoped (one per circuit) so it can attach the
/// current user's bearer token. Never throws for HTTP failures: returns ApiResult instead.
/// </summary>
public class TaskBoardApiClient(IHttpClientFactory httpClientFactory, AuthTokenService auth)
{
    public const string HttpClientName = "TaskBoardApi";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Raised when the API rejects our token (expired/invalid); the UI should go to /login.</summary>
    public event Action? SessionExpired;

    // ---- Auth -------------------------------------------------------------

    public async Task<ApiResult<bool>> RegisterAsync(string email, string password)
    {
        var response = await SendAsync(HttpMethod.Post, "api/auth/register", new { email, password });
        return response.IsSuccessStatusCode
            ? ApiResult<bool>.Success(true)
            : ApiResult<bool>.Failure(await ReadErrorAsync(response));
    }

    public async Task<ApiResult<bool>> LoginAsync(string email, string password)
    {
        var response = await SendAsync(HttpMethod.Post, "api/auth/login", new { email, password });
        if (!response.IsSuccessStatusCode)
        {
            return response.StatusCode == HttpStatusCode.Unauthorized
                ? ApiResult<bool>.Failure("Invalid email or password.")
                : ApiResult<bool>.Failure(await ReadErrorAsync(response));
        }

        var token = await response.Content.ReadFromJsonAsync<LoginResponse>(Json);
        if (token?.AccessToken is null)
        {
            return ApiResult<bool>.Failure("The server returned no access token.");
        }

        auth.SignIn(email, token.AccessToken);
        return ApiResult<bool>.Success(true);
    }

    // ---- Projects ---------------------------------------------------------

    public Task<ApiResult<List<ProjectDto>>> GetProjectsAsync() =>
        GetAsync<List<ProjectDto>>("api/projects");

    public Task<ApiResult<ProjectDto>> GetProjectAsync(Guid projectId) =>
        GetAsync<ProjectDto>($"api/projects/{projectId}");

    public Task<ApiResult<ProjectDto>> CreateProjectAsync(CreateProjectRequest request) =>
        WriteAsync<ProjectDto>(HttpMethod.Post, "api/projects", request);

    // ---- Issues -----------------------------------------------------------

    public Task<ApiResult<List<IssueDto>>> GetIssuesAsync(Guid projectId) =>
        GetAsync<List<IssueDto>>($"api/projects/{projectId}/issues");

    public Task<ApiResult<IssueDto>> CreateIssueAsync(Guid projectId, CreateIssueRequest request) =>
        WriteAsync<IssueDto>(HttpMethod.Post, $"api/projects/{projectId}/issues", request);

    public Task<ApiResult<IssueDto>> UpdateIssueStatusAsync(Guid projectId, Guid issueId, IssueStatus status) =>
        WriteAsync<IssueDto>(HttpMethod.Patch, $"api/projects/{projectId}/issues/{issueId}/status", new { status });

    public Task<ApiResult<IssueDto>> UpdateIssueAsync(Guid projectId, Guid issueId, UpdateIssueRequest request) =>
        WriteAsync<IssueDto>(HttpMethod.Put, $"api/projects/{projectId}/issues/{issueId}", request);

    public Task<ApiResult<bool>> DeleteIssueAsync(Guid projectId, Guid issueId) =>
        DeleteAsync($"api/projects/{projectId}/issues/{issueId}");

    // ---- Comments ---------------------------------------------------------

    public Task<ApiResult<List<CommentDto>>> GetCommentsAsync(Guid projectId, Guid issueId) =>
        GetAsync<List<CommentDto>>($"api/projects/{projectId}/issues/{issueId}/comments");

    public Task<ApiResult<CommentDto>> AddCommentAsync(Guid projectId, Guid issueId, string content) =>
        WriteAsync<CommentDto>(HttpMethod.Post, $"api/projects/{projectId}/issues/{issueId}/comments", new CreateCommentRequest(content));

    public Task<ApiResult<bool>> DeleteCommentAsync(Guid projectId, Guid issueId, Guid commentId) =>
        DeleteAsync($"api/projects/{projectId}/issues/{issueId}/comments/{commentId}");

    // ---- Users ------------------------------------------------------------

    public Task<ApiResult<List<UserDto>>> GetUsersAsync() => GetAsync<List<UserDto>>("api/users");

    // ---- Plumbing ---------------------------------------------------------

    private async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        var response = await SendAsync(HttpMethod.Get, url);
        return await ToResultAsync<T>(response);
    }

    private async Task<ApiResult<T>> WriteAsync<T>(HttpMethod method, string url, object body)
    {
        var response = await SendAsync(method, url, body);
        return await ToResultAsync<T>(response);
    }

    private async Task<ApiResult<bool>> DeleteAsync(string url)
    {
        var response = await SendAsync(HttpMethod.Delete, url);
        return response.IsSuccessStatusCode
            ? ApiResult<bool>.Success(true)
            : ApiResult<bool>.Failure(await ReadErrorAsync(response));
    }

    private async Task<ApiResult<T>> ToResultAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<T>.Failure(await ReadErrorAsync(response));
        }

        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        return value is null
            ? ApiResult<T>.Failure("The server returned an empty response.")
            : ApiResult<T>.Success(value);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);

        if (auth.AccessToken is { } token)
        {
            request.Headers.Authorization = new("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        }

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            var response = await client.SendAsync(request);

            // A 401 on an authenticated call means the token expired: drop it and tell the UI.
            if (response.StatusCode == HttpStatusCode.Unauthorized && auth.IsAuthenticated)
            {
                auth.SignOut();
                SessionExpired?.Invoke();
            }

            return response;
        }
        catch (HttpRequestException)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{\"error\":\"Cannot reach the TaskBoard API. Is it running?\"}", Encoding.UTF8, "application/json")
            };
        }
        catch (TaskCanceledException)
        {
            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
            {
                Content = new StringContent("{\"error\":\"The API took too long to respond (is the database waking up?). Try again.\"}", Encoding.UTF8, "application/json")
            };
        }
    }

    /// <summary>Turns the various error shapes the API can return into one readable string.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var fallback = $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";

        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return fallback;
            }

            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            {
                return error.GetString()!;
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                        ? p.Value.EnumerateArray().Select(m => m.GetString())
                        : [p.Value.GetString()])
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                var joined = string.Join(" ", messages);
                if (joined.Length > 0)
                {
                    return joined;
                }
            }

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString()!;
            }
        }
        catch (JsonException)
        {
            // Body wasn't JSON; fall through.
        }

        return fallback;
    }

    private record LoginResponse(string? AccessToken, string? RefreshToken, int ExpiresIn);
}
