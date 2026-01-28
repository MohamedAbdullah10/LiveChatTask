using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LiveChatTask.Pages.User
{
    /// <summary>
    /// Razor Page for user registration.
    /// The actual registration is handled via the Account API using fetch on the .cshtml page.
    /// </summary>
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}

