using System.Security.Claims;
using System.Threading.Tasks;
using LiveChatTask.Models;
using LiveChatTask.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace LiveChatTask.Services
{
    /// <summary>
    /// Abstraction over ASP.NET Core Identity for authentication operations.
    /// </summary>
    public interface IAuthService
    {
        Task<IdentityResult> RegisterUserAsync(RegisterViewModel model, string role);

        Task<SignInResult> PasswordSignInAsync(string emailOrUserName, string password, bool rememberMe);

        Task SignOutAsync();

        Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal);

        Task<bool> IsInRoleAsync(ApplicationUser user, string role);

        Task<ApplicationUser?> FindByEmailOrUserNameAsync(string emailOrUserName);
    }
}

