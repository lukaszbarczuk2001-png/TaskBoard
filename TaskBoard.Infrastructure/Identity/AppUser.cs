using Microsoft.AspNetCore.Identity;

namespace TaskBoard.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
