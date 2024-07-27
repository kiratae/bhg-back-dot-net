using System.ComponentModel.DataAnnotations;

namespace BHG.WebService
{
    public class PostRoomRequest
    {
        [Required]
        public string UserName { get; set; }

        public class Response(Room room)
        {
            public Room Data { get; set; } = room;
        }
    }
}
