using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeautyClinic.Data;
using BeautyClinic.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace BeautyClinic.Pages
{
    [Authorize(Policy = "RequireAuthenticatedUser")]
    public class MyAccountModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public MyAccountModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }
        
        public InputModel Input { get; set; } = new InputModel();

        public UpdateProfileModel Profile { get; set; } = new UpdateProfileModel();

        public ApplicationUser? User { get; set; }

        public string PasswordPlaceholder => new string('*', 8);

        public List<Booking> UpcomingBookings { get; set; } = new List<Booking>();
        public List<Booking> PastBookings { get; set; } = new List<Booking>();

        public class InputModel
        {
            [Required(ErrorMessage = "Stare hasło jest wymagane.")]
            [DataType(DataType.Password)]
            public string? OldPassword { get; set; }

            [Required(ErrorMessage = "Nowe hasło jest wymagane.")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków.")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?""\:{}\|<>\[\]]).+$",
                ErrorMessage = "Hasło musi zawierać wielką literę, małą literę, cyfrę i znak specjalny.")]
            [DataType(DataType.Password)]
            public string? NewPassword { get; set; }

            [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane.")]
            [Compare("NewPassword", ErrorMessage = "Nowe hasło i potwierdzenie nie są identyczne.")]
            [DataType(DataType.Password)]
            public string? ConfirmNewPassword { get; set; }

            [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
            public string Username { get; set; } = string.Empty;
        }

        public class UpdateProfileModel
        {
            [Required(ErrorMessage = "Imię jest wymagane.")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nazwisko jest wymagane.")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Numer telefonu jest wymagany.")]
            [Phone(ErrorMessage = "Nieprawidłowy numer telefonu.")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email jest wymagany.")]
            [EmailAddress(ErrorMessage = "Nieprawidłowy adres email.")]
            public string Email { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Musisz być zalogowany, aby zobaczyć swoje konto.";
                return RedirectToPage("/Login");
            }
            User = user;

            var userId = user.Email;
            var bookings = await _context.Bookings
                .Include(b => b.Service)
                .Include(b => b.Employee)
                .Where(b => b.UserId == userId)
                .ToListAsync();

            var now = DateTime.UtcNow.ToLocalTime();

            UpcomingBookings = bookings
                .Where(b => b.Service != null && (b.Date + b.Time + TimeSpan.FromMinutes(b.Service.DurationMinutes) >= now))
                .ToList();
            PastBookings = bookings
                .Where(b => b.Service != null && (b.Date + b.Time + TimeSpan.FromMinutes(b.Service.DurationMinutes) < now))
                .ToList();

            ViewData["AllBookings"] = bookings
                .Where(b => b.Service != null)
                .Select(b => new
                {
                    b.Id,
                    Date = b.Date.ToString("yyyy-MM-dd"),
                    Time = b.Time.ToString(@"hh\:mm\:ss"),
                    ServiceName = b.Service?.Name ?? "Brak nazwy",
                    Price = b.Service?.Price ?? 0.0m,
                    DurationMinutes = b.Service?.DurationMinutes ?? 0,
                    EmployeeFirstName = b.Employee?.FirstName ?? "Brak pracownika",
                    EmployeeLastName = b.Employee?.LastName ?? ""
                }).ToList();

            Input.Username = user.Email ?? string.Empty;
            Profile.FirstName = user.FirstName ?? string.Empty;
            Profile.LastName = user.LastName ?? string.Empty;
            Profile.PhoneNumber = user.PhoneNumber ?? string.Empty;
            Profile.Email = user.Email ?? string.Empty;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync([FromBody] UpdateProfileModel profile)
        {
            if (profile == null)
            {
                return BadRequest(new { error = "Dane profilu są nieprawidłowe lub puste." });
            }

            if (string.IsNullOrWhiteSpace(profile.FirstName) ||
                string.IsNullOrWhiteSpace(profile.LastName) ||
                string.IsNullOrWhiteSpace(profile.PhoneNumber) ||
                string.IsNullOrWhiteSpace(profile.Email))
            {
                var errors = new List<string>();
                if (string.IsNullOrWhiteSpace(profile.FirstName)) errors.Add("Imię jest wymagane.");
                if (string.IsNullOrWhiteSpace(profile.LastName)) errors.Add("Nazwisko jest wymagane.");
                if (string.IsNullOrWhiteSpace(profile.PhoneNumber)) errors.Add("Numer telefonu jest wymagany.");
                if (string.IsNullOrWhiteSpace(profile.Email)) errors.Add("Email jest wymagany.");
                return BadRequest(new { error = "Błąd walidacji", details = errors });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                foreach (var key in ModelState.Keys)
                {
                    var value = ModelState[key];
                }
                return BadRequest(new { error = "Błąd walidacji", details = errors });
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return BadRequest(new { error = "Użytkownik nie znaleziony" });
            }

            try
            {
                user.FirstName = profile.FirstName;
                user.LastName = profile.LastName;
                user.PhoneNumber = profile.PhoneNumber;

                if (!string.Equals(user.Email, profile.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var emailResult = await _userManager.SetEmailAsync(user, profile.Email);
                    if (!emailResult.Succeeded)
                    {
                        return BadRequest(new { error = "Błąd podczas aktualizacji adresu e-mail", details = emailResult.Errors.Select(e => e.Description) });
                    }
                    user.UserName = profile.Email;
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return BadRequest(new { error = "Błąd podczas aktualizacji profilu", details = updateResult.Errors.Select(e => e.Description) });
                }

                var bookings = await _context.Bookings
                    .Where(b => b.UserId == user.UserName)
                    .ToListAsync();
                foreach (var booking in bookings)
                {
                    booking.FirstName = profile.FirstName;
                    booking.LastName = profile.LastName;
                }
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Wewnętrzny błąd serwera", details = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostUpdatePasswordAsync([FromBody] InputModel input)
        {
            if (input == null)
            {
                return BadRequest(new { error = "Dane wejściowe są nieprawidłowe lub puste." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { error = "Błąd walidacji", details = errors });
            }

            var user = await _userManager.FindByNameAsync(input.Username);
            if (user == null)
            {
                return BadRequest(new { error = "Nie znaleziono użytkownika." });
            }

            var passwordCheck = await _userManager.CheckPasswordAsync(user, input.OldPassword);
            if (!passwordCheck)
            {
                return BadRequest(new { error = "Nieprawidłowe stare hasło." });
            }

            var result = await _userManager.ChangePasswordAsync(user, input.OldPassword, input.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = "Błąd podczas zmiany hasła", details = result.Errors.Select(e => e.Description) });
            }

            await _signInManager.RefreshSignInAsync(user);
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostCancelBookingAsync([FromForm] int bookingId)
        {
            if (bookingId == 0)
            {
                return BadRequest(new { error = "Nieprawidłowy bookingId." });
            }

            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return NotFound(new { error = "Rezerwacja nie znaleziona." });
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return BadRequest(new { error = "Użytkownik nie jest zalogowany." });
            }

            if (booking.UserId != user.UserName)
            {
                return BadRequest(new { error = "Rezerwacja nie należy do użytkownika." });
            }

            _context.Bookings.Remove(booking);
            try
            {
                int affectedRows = await _context.SaveChangesAsync();
                if (affectedRows == 0)
                {
                    return StatusCode(500, new { error = "Nie udało się usunąć rezerwacji." });
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Błąd podczas anulowania rezerwacji.", details = ex.Message });
            }
        }
    }
}