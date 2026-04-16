using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BeautyClinic.Data;
using BeautyClinic.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BeautyClinic.Pages
{
    public class AppointmentBookingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AppointmentBookingModel> _logger;

        public AppointmentBookingModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<AppointmentBookingModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty]
        public Booking Booking { get; set; } = new();

        [BindProperty]
        public List<SelectListItem> Services { get; set; } = new List<SelectListItem>();

        [BindProperty]
        public List<ApplicationUser> Employees { get; set; } = new List<ApplicationUser>();

        public int? BookingId { get; set; }
        public List<DateTime> BookedSlots { get; set; } = new List<DateTime>();
        public Service Service { get; set; }
        public List<string> AvailableSlots { get; set; } = new List<string>();
        public string SelectedEmployeeId { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int? bookingId, int? serviceId)
        {
            try
            {
                BookingId = bookingId;

                var user = await _userManager.GetUserAsync(HttpContext.User);
                if (user == null)
                {
                    return RedirectToPage("/Login");
                }

                Services = await _context.Services
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name ?? "Brak nazwy" })
                    .ToListAsync();

                var employeeRoleId = "32efc263-254f-4eb0-bc95-c8cf0877041d";
                var employeeUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == employeeRoleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                Employees = await _context.Users
                    .Where(u => employeeUserIds.Contains(u.Id) && u.FirstName != null && u.LastName != null)
                    .Select(u => new ApplicationUser { Id = u.Id, FirstName = u.FirstName, LastName = u.LastName })
                    .ToListAsync();

                if (bookingId.HasValue)
                {
                    Booking = await _context.Bookings
                        .Include(b => b.Service)
                        .FirstOrDefaultAsync(m => m.Id == bookingId.Value) ?? new Booking();
                    if (Booking.Id == 0) return NotFound($"Wizyta o ID {bookingId.Value} nie istnieje.");
                    if (Booking.UserId != user.Email) return Forbid();
                    Service = Booking.Service;
                    SelectedEmployeeId = Booking.EmployeeId ?? Employees.FirstOrDefault()?.Id ?? "";
                }
                else if (serviceId.HasValue)
                {
                    Service = await _context.Services.FindAsync(serviceId.Value);
                    if (Service == null) return NotFound($"Usługa o ID {serviceId.Value} nie istnieje.");

                    Booking = new Booking
                    {
                        Id = 0,
                        Date = DateTime.Today,
                        Time = TimeSpan.Zero,
                        UserId = user.Email,
                        ServiceId = serviceId.Value,
                        Service = Service,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        PhoneNumber = user.PhoneNumber ?? "",
                        EmployeeId = Employees.FirstOrDefault()?.Id
                    };
                    SelectedEmployeeId = Employees.FirstOrDefault()?.Id ?? "";
                }
                else
                {
                    return RedirectToPage("/ServiceList");
                }

                BookedSlots = await _context.Bookings
                    .Where(b => b.Date >= DateTime.Today && b.Date <= DateTime.Today.AddMonths(2))
                    .Select(b => b.Date + b.Time)
                    .ToListAsync();

                AvailableSlots = await GetAvailableSlotsForDate(Booking.Date.ToString("yyyy-MM-dd"), SelectedEmployeeId);

                return Page();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> OnPostAsync(int? bookingId)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return new JsonResult(new { error = "Nieprawidłowe dane formularza", details = errors }) { StatusCode = 400 };
                }

                var user = await _userManager.GetUserAsync(HttpContext.User);
                if (user == null)
                {
                    return new JsonResult(new { error = "Użytkownik nie jest zalogowany" }) { StatusCode = 401 };
                }
                Booking.UserId = user.Email;
                if (string.IsNullOrEmpty(Booking.FirstName)) Booking.FirstName = user.FirstName ?? "";
                if (string.IsNullOrEmpty(Booking.LastName)) Booking.LastName = user.LastName ?? "";
                if (string.IsNullOrEmpty(Booking.PhoneNumber)) Booking.PhoneNumber = user.PhoneNumber ?? "";

                if (string.IsNullOrEmpty(Booking.FirstName) || string.IsNullOrEmpty(Booking.LastName) || string.IsNullOrEmpty(Booking.PhoneNumber))
                {
                    return new JsonResult(new { error = "Wymagane pola: imię, nazwisko, numer telefonu" }) { StatusCode = 400 };
                }

                var service = await _context.Services.FindAsync(Booking.ServiceId);
                if (service == null)
                {
                    return new JsonResult(new { error = $"Usługa o ID {Booking.ServiceId} nie istnieje" }) { StatusCode = 400 };
                }

                var employee = await _context.Users.FindAsync(Booking.EmployeeId);
                if (employee == null)
                {
                    return new JsonResult(new { error = $"Pracownik o ID {Booking.EmployeeId} nie istnieje" }) { StatusCode = 400 };
                }

                if (Booking.Date < DateTime.Today)
                {
                    return new JsonResult(new { error = "Nieprawidłowa data" }) { StatusCode = 400 };
                }

                var slotDateTime = Booking.Date + Booking.Time;
                if (await _context.Bookings.AnyAsync(b => b.Date == Booking.Date && b.Time == Booking.Time && b.Id != (bookingId ?? 0) && b.EmployeeId == Booking.EmployeeId))
                {
                    return new JsonResult(new { error = "Slot zajęty" }) { StatusCode = 400 };
                }

                if (bookingId.HasValue && bookingId > 0)
                {
                    var existingBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
                    if (existingBooking == null)
                    {
                        return new JsonResult(new { error = $"Wizyta o ID {bookingId} nie istnieje" }) { StatusCode = 404 };
                    }
                    if (existingBooking.UserId != user.Email)
                    {
                        return new JsonResult(new { error = "Brak uprawnień" }) { StatusCode = 403 };
                    }
                    existingBooking.Date = Booking.Date;
                    existingBooking.Time = Booking.Time;
                    existingBooking.ServiceId = Booking.ServiceId;
                    existingBooking.EmployeeId = Booking.EmployeeId;
                    existingBooking.FirstName = Booking.FirstName;
                    existingBooking.LastName = Booking.LastName;
                    existingBooking.PhoneNumber = Booking.PhoneNumber;
                    existingBooking.UserId = Booking.UserId;

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        var errorMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                        return new JsonResult(new { error = "Błąd zapisu do bazy danych", details = errorMessage }) { StatusCode = 500 };
                    }
                }
                else
                {
                    Booking.Id = 0; 
                    _context.Bookings.Add(Booking);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        var errorMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                        return new JsonResult(new { error = "Błąd zapisu do bazy danych", details = errorMessage }) { StatusCode = 500 };
                    }
                }

                return new JsonResult(new { redirectUrl = Url.Page("/MyAccount", null, null, Request.Scheme, null, "appointments") }) { StatusCode = 200 };
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = "Błąd serwera", details = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnGetGetSlotsAsync(string date, string employeeId)
        {
            try
            {
                var availableSlots = await GetAvailableSlotsForDate(date, employeeId);
                return new JsonResult(availableSlots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }


        private async Task<List<string>> GetAvailableSlotsForDate(string date, string employeeId)
        {
            try
            {
                if (!DateTime.TryParse(date, out var parsedDate))
                {
                    return new List<string>();
                }

                var dispositions = await _context.Dispositions
                    .Where(d => d.Date.Date == parsedDate.Date
                             && d.EmployeeId == employeeId
                             && !d.IsUnavailable
                             && d.StartTime.HasValue
                             && d.EndTime.HasValue)
                    .ToListAsync();

                if (!dispositions.Any())
                {
                    return new List<string>();
                }

                var minStartTime = dispositions.Min(d => d.StartTime.Value);
                var maxEndTime = dispositions.Max(d => d.EndTime.Value);

                var availableTimes = new List<string>();
                var currentTime = minStartTime;

                while (currentTime < maxEndTime)
                {
                    availableTimes.Add(currentTime.ToString(@"hh\:mm"));
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(15));
                }

                if (maxEndTime.Minutes == 0)
                {
                    availableTimes.Add(maxEndTime.ToString(@"hh\:mm"));
                }

                var bookedSlots = await _context.Bookings
                    .Where(b => b.Date.Date == parsedDate.Date && b.EmployeeId == employeeId)
                    .Select(b => b.Time.ToString(@"hh\:mm"))
                    .ToListAsync();

                availableTimes = availableTimes.Except(bookedSlots).ToList();
                return availableTimes.OrderBy(t => t).ToList();
            }
            catch (Exception ex)
            {
                return new List<string>();
            }
        }
    }
}