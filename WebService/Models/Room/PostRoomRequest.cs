using System.ComponentModel.DataAnnotations;

namespace BHG.WebService
{
    public class PostRoomRequest
    {
        [Required]
        public string UserName { get; set; }

        public class Response
        {
            public string RoomCode { get; set; }

            public string HostUserName { get; set; }
        }
    }
}
