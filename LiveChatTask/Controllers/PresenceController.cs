using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LiveChatTask.Contracts.Presence;
using LiveChatTask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveChatTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PresenceController : ControllerBase
    {
        private readonly IPresenceService _presenceService;

        public PresenceController(IPresenceService presenceService)
        {
            _presenceService = presenceService;
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest? request = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var role = User.IsInRole("Admin") ? "Admin" : "User";
            await _presenceService.UpdateHeartbeatAsync(userId, role);
            return Ok(new { ok = true });
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var list = await _presenceService.GetUserPresenceListAsync();
            var dto = list.Select(x => new UserPresenceDto
            {
                UserId = x.UserId,
                UserNameOrEmail = x.NameOrEmail,
                Status = x.Status.ToString(),
                LastSeen = x.LastSeen
            });
            return Ok(dto);
        }
    }
}

