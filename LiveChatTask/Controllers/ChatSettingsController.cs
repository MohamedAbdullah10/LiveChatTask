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
                MaxSessionDurationMinutes = settings.MaxSessionDurationMinutes,
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

            var settings = await _settingsService.GetAsync();

            // Update MaxUserMessageLength if provided
            if (request.MaxUserMessageLength.HasValue)
            {
                settings = await _settingsService.UpdateMaxUserMessageLengthAsync(request.MaxUserMessageLength.Value, adminId);
            }

            // Update MaxSessionDurationMinutes if provided
            if (request.MaxSessionDurationMinutes.HasValue)
            {
                settings = await _settingsService.UpdateMaxSessionDurationMinutesAsync(request.MaxSessionDurationMinutes.Value, adminId);
            }

            return Ok(new ChatSettingsResponse
            {
                MaxUserMessageLength = settings.MaxUserMessageLength,
                MaxSessionDurationMinutes = settings.MaxSessionDurationMinutes,
                UpdatedAt = settings.UpdatedAt
            });
        }
    }
}

