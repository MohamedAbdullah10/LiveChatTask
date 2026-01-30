using System.Linq;
using System.Threading.Tasks;
using LiveChatTask.Models;
using LiveChatTask.Models.Auth;
using LiveChatTask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LiveChatTask.Controllers
{
   
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(IAuthService authService, UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

       
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            var result = await _authService.RegisterUserAsync(model, role: "User");

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Registration failed",
                    errors = result.Errors.Select(e => e.Description).ToArray()
                });
            }

            return Ok(new { message = "Registration successful" });
        }

      
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            var signInResult = await _authService.PasswordSignInAsync(
                model.EmailOrUserName,
                model.Password,
                model.RememberMe);

            if (!signInResult.Succeeded)
            {
                return Unauthorized(new { message = "Invalid login attempt" });
            }

            var user = await _authService.FindByEmailOrUserNameAsync(model.EmailOrUserName);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : null;

            var redirectUrl = "/";
            if (roles != null && roles.Contains("Admin"))
            {
                redirectUrl = "/Admin/Index";
            }
            else
            {
                redirectUrl = "/User/Chat";
            }

            return Ok(new
            {
                message = "Login successful",
                userName = user?.UserName,
                roles,
                redirectUrl
            });
        }

      
        [HttpPost("user-login")]
        [AllowAnonymous]
        public async Task<IActionResult> UserLogin([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            var signInResult = await _authService.UserPasswordSignInAsync(
                model.EmailOrUserName,
                model.Password,
                model.RememberMe);

            if (!signInResult.Succeeded)
            {
                return Unauthorized(new { message = "Invalid login attempt or you are not authorized as a user" });
            }

            var user = await _authService.FindByEmailOrUserNameAsync(model.EmailOrUserName);
            
            return Ok(new
            {
                message = "Login successful",
                userName = user?.UserName,
                redirectUrl = "/User/Chat"
            });
        }

        
        [HttpPost("admin-login")]
        [AllowAnonymous]
        public async Task<IActionResult> AdminLogin([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            var signInResult = await _authService.AdminPasswordSignInAsync(
                model.EmailOrUserName,
                model.Password,
                model.RememberMe);

            if (!signInResult.Succeeded)
            {
                return Unauthorized(new { message = "Invalid login attempt or you are not authorized as an admin" });
            }

            var user = await _authService.FindByEmailOrUserNameAsync(model.EmailOrUserName);
            
            return Ok(new
            {
                message = "Login successful",
                userName = user?.UserName,
                redirectUrl = "/Admin/Index"
            });
        }

        /// <summary>
        /// Logs the current user out.
        /// POST: /api/account/logout
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.SignOutAsync();
            return Ok(new { message = "Logged out" });
        }
    }
}

