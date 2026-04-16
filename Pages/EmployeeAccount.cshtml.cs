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
    [Authorize(Policy = "RequireEmployeeRole")]
    public class EmployeeAccountModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public EmployeeAccountModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
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

            [Required]
            public string Username { get; set; } = null!;
        }

        public class UpdateProfileModel
        {
            [Required(ErrorMessage = "Imię jest wymagane.")]
            public string FirstName { get; set; } = null!;

            [Required(ErrorMessage = "Nazwisko jest wymagane.")]
            public string LastName { get; set; } = null!;

            [Required(ErrorMessage = "Numer telefonu jest wymagany.")]
            [Phone(ErrorMessage = "Nieprawidłowy numer telefonu.")]
            public string PhoneNumber { get; set; } = null!;

            [Required(ErrorMessage = "Email jest wymagany.")]
            [EmailAddress(ErrorMessage = "Nieprawidłowy adres email.")]
            public string Email { get; set; } = null!;
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
                .Where(b => b.UserId == userId)
                .ToListAsync();

            var now = DateTime.UtcNow.ToLocalTime();

            UpcomingBookings = bookings
                .Where(b => b.Date + b.Time + TimeSpan.FromMinutes(b.Service?.DurationMinutes ?? 0) >= now)
                .ToList();
            PastBookings = bookings
                .Where(b => b.Date + b.Time + TimeSpan.FromMinutes(b.Service?.DurationMinutes ?? 0) < now)
                .ToList();

            ViewData["AllBookings"] = bookings.Select(b => new
            {
                b.Id,
                Date = b.Date.ToString("yyyy-MM-dd"),
                Time = b.Time.ToString(@"hh\:mm\:ss"),
                ServiceName = b.Service?.Name,
                Price = b.Service?.Price ?? 0.0m,
                DurationMinutes = b.Service?.DurationMinutes ?? 0
            }).ToList();

            Input.Username = user.Email;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync([FromBody] UpdateProfileModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return BadRequest("Użytkownik nie znaleziony.");
            }

            try
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                if (user.Email != model.Email)
                {
                    var emailResult = await _userManager.SetEmailAsync(user, model.Email);
                    if (!emailResult.Succeeded)
                    {
                        return BadRequest("Błąd podczas aktualizacji adresu e-mail.");
                    }
                    var usernameResult = await _userManager.SetUserNameAsync(user, model.Email);
                    if (!usernameResult.Succeeded)
                    {
                        return BadRequest("Błąd podczas aktualizacji nazwy użytkownika.");
                    }
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return BadRequest("Błąd podczas aktualizacji danych użytkownika.");
                }

                var bookings = await _context.Bookings
                    .Where(b => b.UserId == user.Email)
                    .ToListAsync();

                if (bookings.Any())
                {
                    foreach (var booking in bookings)
                    {
                        booking.FirstName = model.FirstName;
                        booking.LastName = model.LastName;
                        booking.PhoneNumber = model.PhoneNumber;
                        _context.Update(booking);
                    }

                    var changes = await _context.SaveChangesAsync();
                }
                else
                {
                    Debug.WriteLine("Brak rezerwacji do aktualizacji.");
                }

                await _signInManager.RefreshSignInAsync(user);
                return new OkResult();
            }
            catch (Exception ex)
            {
                return BadRequest("Wystąpił błąd podczas aktualizacji profilu.");
            }
        }

        public async Task<IActionResult> OnPostUpdatePhoneAsync(string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null) return BadRequest();
            user.PhoneNumber = phoneNumber;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            return new OkResult();
        }

        public async Task<IActionResult> OnPostUpdateEmailAsync(string email)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null) return BadRequest();
            var result = await _userManager.SetEmailAsync(user, email);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return new OkResult();
            }
            return BadRequest();
        }

        public async Task<IActionResult> OnPostUpdatePasswordAsync([FromBody] InputModel input)
        {
            try
            {
                if (input == null)
                {
                    return new JsonResult(new { error = "Nieprawidłowe dane wejściowe." });
                }
                if (string.IsNullOrEmpty(input.OldPassword) || string.IsNullOrEmpty(input.NewPassword) || string.IsNullOrEmpty(input.ConfirmNewPassword))
                {
                    return new JsonResult(new { error = "Wypełnij wszystkie pola hasła." });
                }
                if (input.NewPassword != input.ConfirmNewPassword)
                {
                    return new JsonResult(new { error = "Nowe hasło i potwierdzenie nie są identyczne." });
                }

                var user = await _userManager.FindByEmailAsync(input.Username);
                if (user == null)
                {
                    return new JsonResult(new { error = "Użytkownik nie istnieje. Sprawdź dane." });
                }

                var passwordCheck = await _userManager.CheckPasswordAsync(user, input.OldPassword);
                if (!passwordCheck)
                {
                    return new JsonResult(new { error = "Stare hasło jest nieprawidłowe." });
                }

                var result = await _userManager.ChangePasswordAsync(user, input.OldPassword, input.NewPassword);

                if (result.Succeeded)
                {
                    var updatedUser = await _userManager.FindByIdAsync(user.Id);
                    if (updatedUser != null)
                    {
                        await _context.SaveChangesAsync();
                        var newPasswordCheck = await _userManager.CheckPasswordAsync(updatedUser, input.NewPassword);
                        if (!newPasswordCheck)
                        {
                            return new JsonResult(new { error = "Wystąpił problem z zapisem nowego hasła." });
                        }
                    }

                    await _signInManager.SignOutAsync();
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return new JsonResult(new { success = true });
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                return new JsonResult(new { error = string.Join(", ", errors) });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = "Wystąpił wewnętrzny błąd serwera." });
            }
        }
    }
}