using System.ComponentModel.DataAnnotations;

namespace LiveChatTask.Models.Auth
{
    /// <summary>
    /// DTO for login requests (used by API and Razor Pages).
    /// </summary>
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email or Username")]
        public string EmailOrUserName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

