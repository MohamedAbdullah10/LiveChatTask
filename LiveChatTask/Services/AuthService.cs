using System.Security.Claims;
using System.Threading.Tasks;
using LiveChatTask.Models;
using LiveChatTask.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace LiveChatTask.Services
{
    
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

       
        public async Task<SignInResult> PasswordSignInAsync(string emailOrUserName, string password, bool rememberMe)
        {
            var user = await FindByEmailOrUserNameAsync(emailOrUserName);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            return await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
        }

        
        public async Task<SignInResult> UserPasswordSignInAsync(string emailOrUserName, string password, bool rememberMe)
        {
            var user = await FindByEmailOrUserNameAsync(emailOrUserName);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            // Check if user has "User" role
            var isUser = await _userManager.IsInRoleAsync(user, "User");
            if (!isUser)
            {
                return SignInResult.Failed;
            }

            return await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
        }

        
        public async Task<SignInResult> AdminPasswordSignInAsync(string emailOrUserName, string password, bool rememberMe)
        {
            var user = await FindByEmailOrUserNameAsync(emailOrUserName);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            // Check if user has "Admin" role
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (!isAdmin)
            {
                return SignInResult.Failed;
            }

            return await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
        }

        
        public Task SignOutAsync() => _signInManager.SignOutAsync();

        
        public Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal)
            => _userManager.GetUserAsync(principal);

       
        public Task<bool> IsInRoleAsync(ApplicationUser user, string role)
            => _userManager.IsInRoleAsync(user, role);

       
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

