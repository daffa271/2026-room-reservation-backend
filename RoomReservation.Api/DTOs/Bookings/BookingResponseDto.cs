using RoomReservation.Api.Entities;

namespace RoomReservation.Api.DTOs.Bookings;

public class BookingResponseDto
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;

    public string BorrowerName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public BookingStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
