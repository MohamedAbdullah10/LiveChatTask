using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LiveChatTask.Pages.Admin
{
    /// <summary>
    /// Razor Page for admin login.
    /// Uses the same Account API as user login but enforces Admin role on the client.
    /// </summary>
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Page("/Admin/Index");
        }
    }
}

