using Microsoft.AspNetCore.Mvc;

namespace BHG.WebService
{
    [ApiController]
    [Route("rooms/{roomCode}/dm-games")]
    public class DMGameController : BaseController
    {
        private readonly ILogger<DMGameController> _logger;

        public DMGameController(ILogger<DMGameController> logger)
        {
            _logger = logger;
        }

        [HttpGet()]
        public ActionResult<string> Get()
        {
            const string func = "Get";
            try
            {
                if (!RouteData.Values.TryGetValue("roomCode", out object? outVal)) return BadRequest();

                string? roomCode = (string?)outVal;
                if (string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                return Ok(roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }
    }
}
