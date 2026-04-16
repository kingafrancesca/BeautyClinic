using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeautyClinic.Models;
using BeautyClinic.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BeautyClinic.Pages
{
    [Authorize(Policy = "RequireEmployeeRole")]
    public class CompanyViewModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public CompanyViewModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public List<Booking> Bookings { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            var isEmployee = await _userManager.IsInRoleAsync(user, "Pracownik");
            if (!isEmployee)
            {
                return Forbid();
            }

            CurrentUser = user;

            Bookings = await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.EmployeeId == CurrentUser.Id)
                .ToListAsync() ?? new List<Booking>();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelBookingAsync([FromForm] int bookingId)
        {
            if (bookingId == 0)
            {
                return BadRequest("Nieprawidłowy bookingId.");
            }

            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return Forbid();
            }

            var isEmployee = await _userManager.IsInRoleAsync(user, "Pracownik");
            if (!isEmployee)
            {
                return Forbid();
            }

            if (booking.EmployeeId != user.Id)
            {
                return Forbid();
            }

            _context.Bookings.Remove(booking);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Błąd podczas anulowania rezerwacji.");
            }

            return new JsonResult(new { success = true });
        }
    }
}