using System.Security.Claims;
using System.Threading.Tasks;
using LiveChatTask.Models;
using LiveChatTask.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace LiveChatTask.Services
{
    /// <summary>
    /// Concrete implementation of IAuthService using ASP.NET Core Identity.
    /// Password hashing and validation are delegated to Identity.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Registers a new user with the specified role.
        /// </summary>
        public async Task<IdentityResult> RegisterUserAsync(RegisterViewModel model, string role)
        {
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                Role = role,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return result;
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);

            return result;
        }

        /// <summary>
        /// Signs in the user using either email or username and password.
        /// </summary>
        public async Task<SignInResult> PasswordSignInAsync(string emailOrUserName, string password, bool rememberMe)
        {
            var user = await FindByEmailOrUserNameAsync(emailOrUserName);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            return await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        public Task SignOutAsync() => _signInManager.SignOutAsync();

        /// <summary>
        /// Retrieves the ApplicationUser corresponding to the given ClaimsPrincipal.
        /// </summary>
        public Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal)
            => _userManager.GetUserAsync(principal);

        /// <summary>
        /// Checks whether the specified user is in the given role.
        /// </summary>
        public Task<bool> IsInRoleAsync(ApplicationUser user, string role)
            => _userManager.IsInRoleAsync(user, role);

        /// <summary>
        /// Finds a user by email or username.
        /// </summary>
        public async Task<ApplicationUser?> FindByEmailOrUserNameAsync(string emailOrUserName)
        {
            var byEmail = await _userManager.FindByEmailAsync(emailOrUserName);
            if (byEmail != null)
            {
                return byEmail;
            }

            return await _userManager.FindByNameAsync(emailOrUserName);
        }
    }
}

