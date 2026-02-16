using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Data;
using RoomReservation.Api.Entities;

namespace RoomReservation.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BookingsController(AppDbContext db)
        {
            _db = db;
        }

        // =========================================================
        // GET: /api/Bookings
        // Filter + Search + Date + Sorting
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetBookings(
            [FromQuery] int? status,
            [FromQuery] int? roomId,
            [FromQuery] string? q,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDir
        )
        {
            var query = _db.Bookings
                .Include(b => b.Room)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(b => (int)b.Status == status.Value);

            if (roomId.HasValue)
                query = query.Where(b => b.RoomId == roomId.Value);

            // SEARCH
            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim().ToLower();
                query = query.Where(b =>
                    (b.BorrowerName ?? "").ToLower().Contains(keyword) ||
                    (b.Purpose ?? "").ToLower().Contains(keyword) ||
                    (b.Room != null ? b.Room.Name : "").ToLower().Contains(keyword)
                );
            }

            // DATE FILTER
            if (dateFrom.HasValue)
                query = query.Where(b => b.StartTime >= dateFrom.Value.Date);

            if (dateTo.HasValue)
                query = query.Where(b => b.StartTime < dateTo.Value.Date.AddDays(1));

            // SORTING
            var dir = (sortDir ?? "desc").ToLower() == "asc" ? "asc" : "desc";
            var sb = (sortBy ?? "startTime").ToLower();

            query = (sb, dir) switch
            {
                ("endtime", "asc") => query.OrderBy(b => b.EndTime),
                ("endtime", "desc") => query.OrderByDescending(b => b.EndTime),

                ("status", "asc") => query.OrderBy(b => b.Status),
                ("status", "desc") => query.OrderByDescending(b => b.Status),

                ("borrowername", "asc") => query.OrderBy(b => b.BorrowerName),
                ("borrowername", "desc") => query.OrderByDescending(b => b.BorrowerName),

                ("roomname", "asc") => query.OrderBy(b => b.Room != null ? b.Room.Name : ""),
                ("roomname", "desc") => query.OrderByDescending(b => b.Room != null ? b.Room.Name : ""),


                ("starttime", "asc") => query.OrderBy(b => b.StartTime),
                _ => query.OrderByDescending(b => b.StartTime),
            };

            var data = await query
                .Select(b => new
                {
                    b.Id,
                    b.RoomId,
                    RoomName = b.Room != null ? b.Room.Name : "-",
                    b.BorrowerName,
                    b.Purpose,
                    b.StartTime,
                    b.EndTime,
                    Status = (int)b.Status
                })
                .ToListAsync();

            return Ok(data);
        }

        // =========================================================
        // DETAIL
        // =========================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            var b = await _db.Bookings
                .Include(x => x.Room)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (b == null)
                return NotFound("Booking tidak ditemukan.");

            return Ok(new
            {
                b.Id,
                b.RoomId,
                RoomName = b.Room != null ? b.Room.Name : "-",
                b.BorrowerName,
                b.Purpose,
                b.StartTime,
                b.EndTime,
                Status = (int)b.Status
            });
        }

        // =========================================================
        // CREATE
        // =========================================================
        public class CreateBookingDto
        {
            public int RoomId { get; set; }
            public string BorrowerName { get; set; } = "";
            public string Purpose { get; set; } = "";
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            if (dto.EndTime <= dto.StartTime)
                return BadRequest("EndTime harus lebih besar dari StartTime.");

            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == dto.RoomId);
            if (room == null)
                return BadRequest("Room tidak ditemukan.");

            if (!room.IsActive)
                return BadRequest("Room tidak aktif, tidak bisa dibooking.");

            var conflict = await _db.Bookings.AnyAsync(b =>
                b.RoomId == dto.RoomId &&
                b.Status != BookingStatus.Rejected &&
                dto.StartTime < b.EndTime &&
                dto.EndTime > b.StartTime
            );

            if (conflict)
                return BadRequest("Jadwal bentrok: ruangan sudah dibooking pada rentang waktu tersebut.");

            var booking = new Booking
            {
                RoomId = dto.RoomId,
                BorrowerName = dto.BorrowerName,
                Purpose = dto.Purpose,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = BookingStatus.Pending
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            return Ok(new { booking.Id });
        }

        // =========================================================
        // UPDATE
        // =========================================================
        public class UpdateBookingDto
        {
            public int RoomId { get; set; }
            public string BorrowerName { get; set; } = "";
            public string Purpose { get; set; } = "";
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] UpdateBookingDto dto)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null)
                return NotFound("Booking tidak ditemukan.");

            if (dto.EndTime <= dto.StartTime)
                return BadRequest("EndTime harus lebih besar dari StartTime.");

            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == dto.RoomId);
            if (room == null)
                return BadRequest("Room tidak ditemukan.");

            if (!room.IsActive)
                return BadRequest("Room tidak aktif, tidak bisa dibooking.");

            var conflict = await _db.Bookings.AnyAsync(b =>
                b.Id != id &&
                b.RoomId == dto.RoomId &&
                b.Status != BookingStatus.Rejected &&
                dto.StartTime < b.EndTime &&
                dto.EndTime > b.StartTime
            );

            if (conflict)
                return BadRequest("Jadwal bentrok: ruangan sudah dibooking pada rentang waktu tersebut.");

            booking.RoomId = dto.RoomId;
            booking.BorrowerName = dto.BorrowerName;
            booking.Purpose = dto.Purpose;
            booking.StartTime = dto.StartTime;
            booking.EndTime = dto.EndTime;

            await _db.SaveChangesAsync();
            return Ok();
        }

        // =========================================================
        // DELETE
        // =========================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null)
                return NotFound("Booking tidak ditemukan.");

            _db.Bookings.Remove(booking);
            await _db.SaveChangesAsync();

            return Ok();
        }

        // =========================================================
        // UPDATE STATUS
        // =========================================================
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] int status)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null)
                return NotFound("Booking tidak ditemukan.");

            if (status < 0 || status > 2)
                return BadRequest("Status tidak valid.");

            booking.Status = (BookingStatus)status;
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
