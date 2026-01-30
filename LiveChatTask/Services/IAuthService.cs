using System.Security.Claims;
using System.Threading.Tasks;
using LiveChatTask.Models;
using LiveChatTask.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace LiveChatTask.Services
{
   
    public interface IAuthService
    {
        Task<IdentityResult> RegisterUserAsync(RegisterViewModel model, string role);

        Task<SignInResult> PasswordSignInAsync(string emailOrUserName, string password, bool rememberMe);

        Task<SignInResult> UserPasswordSignInAsync(string emailOrUserName, string password, bool rememberMe);

        Task<SignInResult> AdminPasswordSignInAsync(string emailOrUserName, string password, bool rememberMe);

        Task SignOutAsync();

        Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal);

        Task<bool> IsInRoleAsync(ApplicationUser user, string role);

        Task<ApplicationUser?> FindByEmailOrUserNameAsync(string emailOrUserName);
    }
}

