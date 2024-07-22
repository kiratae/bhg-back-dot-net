
using Microsoft.AspNetCore.Mvc;
using CrypticWizard.RandomWordGenerator;
using static CrypticWizard.RandomWordGenerator.WordGenerator;
using Microsoft.AspNetCore.SignalR;

namespace BHG.WebService
{
    [ApiController]
    [Route("rooms")]
    public class RoomController : BaseController
    {
        private readonly ILogger<RoomController> _logger;
        private readonly IHubContext<GameHub> _gameHubContext;
        private static readonly WordGenerator _wordGenerator = new();
        private static readonly List<PartOfSpeech> _wordPattern = [PartOfSpeech.adj, PartOfSpeech.noun, PartOfSpeech.verb];

        public RoomController(ILogger<RoomController> logger, IHubContext<GameHub> gameHubContext)
        {
            _logger = logger;
            _gameHubContext = gameHubContext;
        }

        [HttpGet("{roomCode}")]
        public ActionResult<string> Get(string roomCode)
        {
            const string func = "Get";
            try
            {
                // TODO: Get room data and users, then return it.

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> Post(string userName, CancellationToken cancellationToken)
        {
            const string func = "Post";
            try
            {
                string roomCode = _wordGenerator.GetPattern(_wordPattern, '-');

                // TODO: Create room and make this user is host of the room.

                await Task.Delay(1, cancellationToken);

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
