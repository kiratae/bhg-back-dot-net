using BHG.WebService.Data;

namespace BHG.WebService
{
    public class Room
    {
        public Room()
        {
            Players = [];
        }

        public GameState GameStateId { get; set; }

        public List<Player> Players { get; protected set; }
    }
}
