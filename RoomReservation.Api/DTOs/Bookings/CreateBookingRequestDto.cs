namespace RoomReservation.Api.DTOs.Bookings;

public class CreateBookingRequestDto
{
    public int RoomId { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
