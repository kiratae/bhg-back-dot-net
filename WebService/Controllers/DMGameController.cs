using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BHG.WebService
{
    [ApiController]
    [Route("rooms/{roomCode}/dm-games")]
    public class DMGameController : BaseController
    {
        private readonly ILogger<DMGameController> _logger;
        private readonly IHubContext<GameHub> _gameHubContext;

        public DMGameController(ILogger<DMGameController> logger, IHubContext<GameHub> gameHubContext)
        {
            _logger = logger;
            _gameHubContext = gameHubContext;
        }

        [HttpGet("/start")]
        public ActionResult<string> Start([FromRoute] string roomCode)
        {
            const string func = "Start";
            try
            {
                if (string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                // Get

                _gameHubContext.Clients.Groups(roomCode).SendAsync("GameState", new { Name = "test" });

                return Ok(roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }

        [HttpGet("/setup")]
        public ActionResult<string> Setup([FromRoute] string roomCode)
        {
            const string func = "Setup";
            try
            {
                if (string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                // Get

                _gameHubContext.Clients.Groups(roomCode).SendAsync("GameState", new { Name = "test" });

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
