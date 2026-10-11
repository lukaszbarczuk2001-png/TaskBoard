namespace TaskBoard.Web.Services;

/// <summary>
/// Holds the signed-in user's bearer token for one Blazor circuit (= one browser tab).
/// Registered as Scoped, so each circuit gets its own instance. The token lives only
/// in server memory: a full page reload starts a new circuit and requires signing in again.
/// </summary>
public class AuthTokenService
{
    public string? AccessToken { get; private set; }
    public string? Email { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    public event Action? Changed;

    public void SignIn(string email, string accessToken)
    {
        Email = email;
        AccessToken = accessToken;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        Email = null;
        AccessToken = null;
        Changed?.Invoke();
    }
}
