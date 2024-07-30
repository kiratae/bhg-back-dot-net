
using System.Text.Json.Serialization;

namespace BHG.WebService
{
    public class Player
    {
        public Player()
        {
            RoleId = PlayerRole.Unknown;
            StatusId = PlayerStatus.Unknown;
        }

        public Player(string userName) : this()
        {
            UserName = userName;
        }

        public string UserName { get; set; }

        public PlayerRole RoleId { get; set; }

        public PlayerStatus StatusId { get; set; }

        public bool IsHost { get; set; }

        public bool IsProtected { get; set; }

        [JsonIgnore]
        public bool IsVoted { get; set; }
    }
}
