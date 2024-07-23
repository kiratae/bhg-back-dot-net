
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

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<RoomRequest.Response>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<RoomRequest.Response> Post([FromBody] RoomRequest model, CancellationToken cancellationToken)
        {
            const string func = "Post";
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                string roomCode = _wordGenerator.GetPattern(_wordPattern, '-');

                var res = new RoomRequest.Response() { RoomCode = roomCode, HostUserName = model.UserName };

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
