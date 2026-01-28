using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LiveChatTask.Pages.User
{
    /// <summary>
    /// Razor Page for user login.
    /// The actual authentication is done via the Account API using fetch on the .cshtml page.
    /// </summary>
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }
    }
}

