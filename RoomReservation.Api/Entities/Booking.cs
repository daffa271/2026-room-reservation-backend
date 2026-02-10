namespace RoomReservation.Api.Entities;

public enum BookingStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class Booking
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
