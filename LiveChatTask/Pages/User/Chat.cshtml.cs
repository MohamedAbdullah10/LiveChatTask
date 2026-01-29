using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LiveChatTask.Models;

namespace LiveChatTask.Pages.User
{
    [Authorize]
    public class ChatModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ChatModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/User/Login");
        }
    }
}
