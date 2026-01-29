using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LiveChatTask.Models;

namespace LiveChatTask.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Admin/Login");
        }
    }
}

