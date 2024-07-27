
namespace BHG.WebService
{
    public class Player
    {
        public Player()
        {
            RoleId = PlayerRole.Unknown;
            StatusId = PlayerStatus.Waiting;
        }

        public Player(string userName) : this()
        {
            UserName = userName;
        }

        public string UserName { get; set; }

        public PlayerRole RoleId { get; set; }

        public PlayerStatus StatusId { get; set; }

        public bool IsHost { get; set; }
    }
}
