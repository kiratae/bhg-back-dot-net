
using Microsoft.AspNetCore.Mvc;
using CrypticWizard.RandomWordGenerator;
using static CrypticWizard.RandomWordGenerator.WordGenerator;
using System.Net.Mime;

namespace BHG.WebService
{
    [ApiController]
    [Route("api/rooms")]
    [ProducesResponseType<RoomResponse>(StatusCodes.Status200OK)]
    [Consumes(MediaTypeNames.Application.Json), Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest), ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class RoomController : BaseController
    {
        private readonly ILogger<RoomController> _logger;
        private static readonly WordGenerator _wordGenerator = new();
        private static readonly List<PartOfSpeech> _wordPattern = [PartOfSpeech.adj, PartOfSpeech.noun, PartOfSpeech.verb];

        public RoomController(ILogger<RoomController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{roomCode}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<RoomResponse> Get([FromRoute] string roomCode)
        {
            const string func = "Get";
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                var room = DyingMessageGameManager.GetInstance().GetRoomSession(roomCode);
                if (room == null) return NotFound();

                return Ok(new RoomResponse(room));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }

        [HttpPost]
        public ActionResult<RoomResponse> Post([FromBody] PostRoomRequest model)
        {
            const string func = "Post";
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                string roomCode = _wordGenerator.GetPattern(_wordPattern, '-');

                var room = DyingMessageGameManager.GetInstance().CreateSession(roomCode, model.UserName);

                return Ok(new RoomResponse(room));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }

        [HttpPost("{roomCode}/join")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<RoomResponse> Join([FromRoute] string roomCode, [FromBody] PostRoomRequest model)
        {
            const string func = "Join";
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                var room = DyingMessageGameManager.GetInstance().JoinRoomSession(roomCode, model.UserName);
                if (room == null) return NotFound();

                return Ok(new RoomResponse(room));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }

        [HttpPost("{roomCode}/config")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<RoomResponse> Config([FromRoute] string roomCode, [FromBody] RoomConfigRequest model)
        {
            const string func = "Config";
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                var instance = DyingMessageGameManager.GetInstance();
                var room = instance.GetRoomSession(roomCode);
                if (room == null) return NotFound();

                instance.ConfigGame(room.RoomCode, model.ExtraRoles);

                return Ok(new RoomResponse(instance.GetRoomSession(roomCode)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }
    }
}
