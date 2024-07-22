
using Microsoft.AspNetCore.Mvc;
using CrypticWizard.RandomWordGenerator;
using static CrypticWizard.RandomWordGenerator.WordGenerator;

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
        public ActionResult<string> Get(string roomCode)
        {
            const string func = "Get";
            try
            {
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{func}: Exception caugth.");
                return Error();
            }
        }

        [HttpPost]
        public ActionResult<string> Post(string userName)
        {
            const string func = "Post";
            try
            {
                string roomCode = _wordGenerator.GetPattern(_wordPattern, '-');
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
