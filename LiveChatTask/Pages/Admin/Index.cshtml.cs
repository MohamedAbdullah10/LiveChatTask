using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LiveChatTask.Pages.Admin
{
    /// <summary>
    /// Placeholder admin dashboard page.
    /// Will later host the live chat UI.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}

