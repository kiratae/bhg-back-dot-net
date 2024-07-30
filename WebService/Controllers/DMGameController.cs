using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Net.Mime;

namespace BHG.WebService
{
    [ApiController]
    [Route("api/dm-game-control")]
    [Consumes(MediaTypeNames.Application.Json), Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest), ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class DMGameController : BaseController
    {
        private readonly ILogger<DMGameController> _logger;
        private readonly IHubContext<GameHub> _hubContext;

        public DMGameController(ILogger<DMGameController> logger, IHubContext<GameHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost("{roomCode}")]
        [ProducesResponseType<RoomResponse>(StatusCodes.Status200OK)]
        public async Task<ActionResult<RoomResponse>> DoAction([FromRoute] string roomCode, [FromBody] PostDMGameRequest model)
        {
            const string func = "DoAction";
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(roomCode)) return BadRequest();

                var gameMan = DyingMessageGameManager.GetInstance();
                var room = gameMan.GetRoomSession(roomCode);
                if (room != null && model.AcionTypeId.HasValue)
                {
                    if (model.AcionTypeId.Value == DMGameAction.StartGame)
                    {
                        if (room.IsHostPlayer(model.UserName))
                        {
                            room = await gameMan.StartGame(roomCode, _hubContext);
                            return Ok(new RoomResponse(room));
                        }
                    }
                    else if (model.AcionTypeId.Value == DMGameAction.DogJarvisChooseTarget)
                    {
                        if (room.IsPlayerInRole(model.UserName, PlayerRole.DogJarvis) && !string.IsNullOrWhiteSpace(model.TargetUserName))
                        {
                            room = await gameMan.DogJarvisChooseTarget(roomCode, model.TargetUserName, _hubContext);
                            return Ok(new RoomResponse(room));
                        }
                    }
                    else if (model.AcionTypeId.Value == DMGameAction.KillerChooseTarget)
                    {
                        if (room.IsPlayerInRole(model.UserName, PlayerRole.Killer) && !string.IsNullOrWhiteSpace(model.TargetUserName))
                        {
                            room = await gameMan.KillerChooseTarget(roomCode, model.TargetUserName, _hubContext);
                            return Ok(new RoomResponse(room));
                        }
                    }
                    else if (model.AcionTypeId.Value == DMGameAction.DeadChooseEvidence)
                    {
                        if (room.IsPlayerInRole(model.UserName, PlayerRole.Killer) && !string.IsNullOrWhiteSpace(model.TargetUserName) && model.TargetCardIds.Count == 1)
                        {
                            room = await gameMan.DeadChooseEvidence(roomCode, model.TargetCardIds.First(), _hubContext);
                            return Ok(new RoomResponse(room));
                        }
                    }
                    else if (model.AcionTypeId.Value == DMGameAction.KillerChooseEvidences)
                    {
                        if (room.IsPlayerInRole(model.UserName, PlayerRole.Killer) && !string.IsNullOrWhiteSpace(model.TargetUserName) && model.TargetCardIds.Count == 2)
                        {
                            room = await gameMan.KillerChooseEvidences(roomCode, model.TargetCardIds, _hubContext);
                            return Ok(new RoomResponse(room));
                        }
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
