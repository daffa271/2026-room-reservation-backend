using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Data;
using RoomReservation.Api.DTOs.Bookings;
using RoomReservation.Api.Entities;

namespace RoomReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BookingsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/Bookings?status=Pending&roomId=1
    [HttpGet]
    public async Task<ActionResult<List<BookingResponseDto>>> GetAll(
        [FromQuery] BookingStatus? status,
        [FromQuery] int? roomId
    )
    {
        var query = _db.Bookings
            .Include(b => b.Room)
            .AsQueryable();

        // Filter status (optional)
        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        // Filter roomId (optional)
        if (roomId.HasValue)
            query = query.Where(b => b.RoomId == roomId.Value);

        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingResponseDto
            {
                Id = b.Id,
                RoomId = b.RoomId,
                RoomName = b.Room != null ? b.Room.Name : "",
                BorrowerName = b.BorrowerName,
                Purpose = b.Purpose,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return Ok(bookings);
    }

    // GET: api/Bookings/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponseDto>> GetById(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return NotFound();

        return Ok(new BookingResponseDto
        {
            Id = booking.Id,
            RoomId = booking.RoomId,
            RoomName = booking.Room != null ? booking.Room.Name : "",
            BorrowerName = booking.BorrowerName,
            Purpose = booking.Purpose,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt
        });
    }

    // POST: api/Bookings
    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> Create(CreateBookingRequestDto dto)
    {
        // Validasi waktu
        if (dto.EndTime <= dto.StartTime)
            return BadRequest("EndTime harus lebih besar dari StartTime.");

        // Pastikan room ada
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == dto.RoomId);
        if (room is null)
            return BadRequest("RoomId tidak valid (ruangan tidak ditemukan).");

        // Cek bentrok jadwal
        var hasConflict = await _db.Bookings.AnyAsync(b =>
            b.RoomId == dto.RoomId &&
            b.Status != BookingStatus.Rejected &&
            dto.StartTime < b.EndTime &&
            dto.EndTime > b.StartTime
        );

        if (hasConflict)
            return BadRequest("Jadwal bentrok: ruangan sudah dibooking pada rentang waktu tersebut.");

        var booking = new Booking
        {
            RoomId = dto.RoomId,
            BorrowerName = dto.BorrowerName,
            Purpose = dto.Purpose,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        var created = await _db.Bookings
            .Include(b => b.Room)
            .FirstAsync(b => b.Id == booking.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new BookingResponseDto
        {
            Id = created.Id,
            RoomId = created.RoomId,
            RoomName = created.Room != null ? created.Room.Name : "",
            BorrowerName = created.BorrowerName,
            Purpose = created.Purpose,
            StartTime = created.StartTime,
            EndTime = created.EndTime,
            Status = created.Status,
            CreatedAt = created.CreatedAt
        });
    }

    // PUT: api/Bookings/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateBookingRequestDto dto)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null) return NotFound();

        if (dto.EndTime <= dto.StartTime)
            return BadRequest("EndTime harus lebih besar dari StartTime.");

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == dto.RoomId);
        if (room is null)
            return BadRequest("RoomId tidak valid (ruangan tidak ditemukan).");

        // Exclude dirinya sendiri
        var hasConflict = await _db.Bookings.AnyAsync(b =>
            b.Id != id &&
            b.RoomId == dto.RoomId &&
            b.Status != BookingStatus.Rejected &&
            dto.StartTime < b.EndTime &&
            dto.EndTime > b.StartTime
        );

        if (hasConflict)
            return BadRequest("Jadwal bentrok: ruangan sudah dibooking pada rentang waktu tersebut.");

        booking.RoomId = dto.RoomId;
        booking.BorrowerName = dto.BorrowerName;
        booking.Purpose = dto.Purpose;
        booking.StartTime = dto.StartTime;
        booking.EndTime = dto.EndTime;
        booking.Status = dto.Status;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PATCH: api/Bookings/5/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateBookingStatusRequestDto dto)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null) return NotFound();

        booking.Status = dto.Status;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Bookings/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null) return NotFound();

        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
