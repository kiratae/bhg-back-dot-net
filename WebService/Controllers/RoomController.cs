
using Microsoft.AspNetCore.Mvc;
using CrypticWizard.RandomWordGenerator;
using static CrypticWizard.RandomWordGenerator.WordGenerator;
using System.Net.Mime;

namespace BHG.WebService
{
    [ApiController]
    [Route("rooms")]
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
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<PostRoomRequest.Response>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<GetRoomRequest.Response> Get([FromRoute] string roomCode, CancellationToken cancellationToken)
        {
            const string func = "Get";
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                var res = new PostRoomRequest.Response();

                return Ok(res);
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

                var res = new PostRoomRequest.Response() { RoomCode = roomCode, HostUserName = model.UserName };

                return Ok(res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }
    }
}
