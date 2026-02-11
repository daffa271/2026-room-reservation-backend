using RoomReservation.Api.Entities;

namespace RoomReservation.Api.DTOs.Bookings;

public class UpdateBookingStatusRequestDto
{
    public BookingStatus Status { get; set; }
}
