using System.ComponentModel.DataAnnotations;

namespace BHG.WebService
{
    public class RoomConfigRequest : BaseModel
    {
        [Required]
        public string UserName { get; set; }

        public List<PlayerRole> ExtraRoles { get; set; }
    }
}
