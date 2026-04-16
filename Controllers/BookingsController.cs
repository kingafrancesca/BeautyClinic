using Microsoft.AspNetCore.Mvc;
using BeautyClinic.Data;
using BeautyClinic.Models;

namespace BeautyClinic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostBooking([FromBody] Booking booking)
        {
            if (booking == null || booking.ServiceId <= 0 || booking.Date == default ||
                string.IsNullOrEmpty(booking.FirstName) || string.IsNullOrEmpty(booking.LastName) ||
                string.IsNullOrEmpty(booking.PhoneNumber))
            {
                return BadRequest("Nieprawidłowe dane rezerwacji.");
            }

            // Konwersja czasu z stringa na TimeSpan
            if (!string.IsNullOrEmpty(booking.Time.ToString()) && booking.Time == default)
            {
                var timeParts = booking.Time.ToString().Split(':');
                if (timeParts.Length == 2)
                {
                    booking.Time = new TimeSpan(int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);
                }
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rezerwacja zapisana pomyślnie." });
        }
    }
}