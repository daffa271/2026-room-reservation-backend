namespace RoomReservation.Api.DTOs.Rooms;

public class UpdateRoomRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
}
