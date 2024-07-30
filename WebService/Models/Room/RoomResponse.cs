namespace BHG.WebService
{
    public class RoomResponse : BaseModel
    {
        public RoomResponse(Room room)
        {
            Data = room;
        }

        public Room Data { get; set; }
    }
}
