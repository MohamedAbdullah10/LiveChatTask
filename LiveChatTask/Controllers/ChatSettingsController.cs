using System.Security.Claims;
using System.Threading.Tasks;
using LiveChatTask.Contracts.Settings;
using LiveChatTask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveChatTask.Controllers
{
    [ApiController]
    [Route("api/settings/chat")]
    [Authorize]
    public class ChatSettingsController : ControllerBase
    {
        private readonly IChatSettingsService _settingsService;

        public ChatSettingsController(IChatSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<ActionResult<ChatSettingsResponse>> Get()
        {
            var settings = await _settingsService.GetAsync();
            return Ok(new ChatSettingsResponse
            {
                MaxUserMessageLength = settings.MaxUserMessageLength,
                UpdatedAt = settings.UpdatedAt
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ChatSettingsResponse>> Update([FromBody] UpdateChatSettingsRequest request)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Unauthorized();
            }

            var updated = await _settingsService.UpdateMaxUserMessageLengthAsync(request.MaxUserMessageLength, adminId);
            return Ok(new ChatSettingsResponse
            {
                MaxUserMessageLength = updated.MaxUserMessageLength,
                UpdatedAt = updated.UpdatedAt
            });
        }
    }
}

