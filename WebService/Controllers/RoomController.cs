
using Microsoft.AspNetCore.Mvc;
using CrypticWizard.RandomWordGenerator;
using static CrypticWizard.RandomWordGenerator.WordGenerator;
using System.Net.Mime;
using Microsoft.AspNetCore.SignalR;

namespace BHG.WebService
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomController : BaseController
    {
        private readonly ILogger<RoomController> _logger;
        private static readonly WordGenerator _wordGenerator = new();
        private static readonly List<PartOfSpeech> _wordPattern = [PartOfSpeech.adj, PartOfSpeech.noun, PartOfSpeech.verb];
        private readonly IHubContext<GameHub> _hubContext;

        public RoomController(ILogger<RoomController> logger, IHubContext<GameHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpGet("{roomCode}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<PostRoomRequest.Response>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<GetRoomRequest.Response> Get([FromRoute] string roomCode, CancellationToken cancellationToken)
        {
            const string func = "Get";
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                var room = DyingMessageGameManager.GetInstance().GetRoomSession(roomCode);

                return Ok(new PostRoomRequest.Response(room));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<PostRoomRequest.Response>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<PostRoomRequest.Response> Post([FromBody] PostRoomRequest model, CancellationToken cancellationToken)
        {
            const string func = "Post";
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                string roomCode = _wordGenerator.GetPattern(_wordPattern, '-');

                var room = DyingMessageGameManager.GetInstance().CreateSession(roomCode, model.UserName);

                return Ok(new PostRoomRequest.Response(room));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }

        [HttpPatch("{roomCode}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<PostRoomRequest.Response>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<PostRoomRequest.Response> Patch([FromRoute] string roomCode, [FromBody] PostRoomRequest model, CancellationToken cancellationToken)
        {
            const string func = "Patch";
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                var room = DyingMessageGameManager.GetInstance().JoinRoomSession(roomCode, model.UserName);

                return Ok(new PostRoomRequest.Response(room));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }

        [HttpPost("{roomCode}/start-game")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<PostRoomRequest.Response>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PostRoomRequest.Response>> StartGame([FromRoute] string roomCode, [FromBody] PostRoomRequest model, CancellationToken cancellationToken)
        {
            const string func = "StartGame";
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                var gameMan = DyingMessageGameManager.GetInstance();
                var room = gameMan.GetRoomSession(roomCode);
                if (room != null)
                {
                    var hostPlayer = room.Players.Find(x => x.UserName == model.UserName);
                    if (hostPlayer.IsHost)
                    {
                        room = await gameMan.StartGame(roomCode, _hubContext);
                        return Ok(new PostRoomRequest.Response(room));
                    }
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }
    }
}
