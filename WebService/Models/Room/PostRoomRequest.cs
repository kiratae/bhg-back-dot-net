using System.ComponentModel.DataAnnotations;

namespace BHG.WebService
{
    public class PostRoomRequest : BaseModel
    {
        [Required]
        public string UserName { get; set; }
    }
}
