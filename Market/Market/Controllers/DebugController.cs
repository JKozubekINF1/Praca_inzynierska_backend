using Market.Services;
using Microsoft.AspNetCore.Mvc;

namespace Market.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly IAiModerationService _aiService;

        public DebugController(IAiModerationService aiService)
        {
            _aiService = aiService;
        }

        [HttpGet("test-openai")]
        public async Task<IActionResult> TestOpenAiConnection()
        {
            var result = await _aiService.TestConnectionAsync();
            return Ok(new { Result = result });
        }
    }
}