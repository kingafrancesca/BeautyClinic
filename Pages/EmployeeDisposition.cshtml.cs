using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeautyClinic.Data;
using BeautyClinic.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BeautyClinic.Pages
{
    [Authorize(Policy = "RequireEmployeeRole")] // Wymaga roli pracownika
    public class EmployeeDispositionModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public EmployeeDispositionModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public ApplicationUser? Employee { get; set; }
        public List<Disposition> Dispositions { get; set; } = new List<Disposition>();

        [BindProperty]
        public DispositionInputModel Input { get; set; } = new DispositionInputModel();

        public class DispositionInputModel
        {
            [Required(ErrorMessage = "Data jest wymagana.")]
            public DateTime Date { get; set; }

            public TimeSpan? StartTime { get; set; }
            public TimeSpan? EndTime { get; set; }

            public bool IsUnavailable { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                var currentUser = await _userManager.GetUserAsync(HttpContext.User);
                if (currentUser == null)
                {
                    return RedirectToPage("/Login");
                }
                id = currentUser.Id;
            }

            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                return NotFound($"Użytkownik o ID {id} nie został znaleziony.");
            }

            Employee = employee;
            Dispositions = await _context.Dispositions
                .Where(d => d.EmployeeId == id)
                .ToListAsync();

            ViewData["Dispositions"] = Dispositions.Select(d => new
            {
                d.Date,
                d.StartTime,
                d.EndTime,
                d.IsUnavailable
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostSaveDispositionAsync([FromBody] DispositionInputModel input, string id = null)
        {
            var employee = await _userManager.GetUserAsync(HttpContext.User);
            if (employee == null)
            {
                return BadRequest(new { error = "Użytkownik nie znaleziony." });
            }

            if (id != null && id != employee.Id)
            {
                employee = await _userManager.FindByIdAsync(id);
                if (employee == null)
                {
                    return NotFound($"Użytkownik o ID {id} nie został znaleziony.");
                }
            }

            // Debugowanie - sprawdź otrzymane dane
            Console.WriteLine($"Received Input: Date={input.Date}, StartTime={input.StartTime}, EndTime={input.EndTime}, IsUnavailable={input.IsUnavailable}");

            // Walidacja po stronie serwera
            if (!input.IsUnavailable && (input.StartTime == null || input.EndTime == null))
            {
                Console.WriteLine($"Validation failed: StartTime={input.StartTime}, EndTime={input.EndTime}, IsUnavailable={input.IsUnavailable}");
                return BadRequest(new { error = "Godziny rozpoczęcia i zakończenia są wymagane, jeśli pracownik jest dostępny." });
            }
            if (!input.IsUnavailable && input.StartTime >= input.EndTime)
            {
                return BadRequest(new { error = "Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { error = "Błąd walidacji", details = errors });
            }

            var disposition = await _context.Dispositions
                .FirstOrDefaultAsync(d => d.EmployeeId == employee.Id && d.Date.Date == input.Date.Date);

            if (disposition == null)
            {
                disposition = new Disposition
                {
                    EmployeeId = employee.Id,
                    Date = input.Date.Date,
                    CreatedAt = DateTime.UtcNow.ToLocalTime(),
                };
                _context.Dispositions.Add(disposition);
            }

            // Aktualizacja istniejącego rekordu
            disposition.IsUnavailable = input.IsUnavailable;
            disposition.StartTime = input.IsUnavailable ? null : input.StartTime;
            disposition.EndTime = input.IsUnavailable ? null : input.EndTime;
            disposition.UpdatedAt = DateTime.UtcNow.ToLocalTime();

            try
            {
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, message = "Dyspozycja zapisana pomyślnie." });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = "Błąd podczas zapisywania do bazy danych.", details = ex.InnerException?.Message });
            }
        }

        public async Task<IActionResult> OnGetGetDispositionsAsync(int month, int year, string id = null)
        {
            var employee = await _userManager.GetUserAsync(HttpContext.User);
            if (employee == null)
            {
                return BadRequest(new { error = "Użytkownik nie znaleziony." });
            }

            if (id != null && id != employee.Id)
            {
                employee = await _userManager.FindByIdAsync(id);
                if (employee == null)
                {
                    return NotFound($"Użytkownik o ID {id} nie został znaleziony.");
                }
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var dispositions = await _context.Dispositions
                .Where(d => d.EmployeeId == employee.Id && d.Date >= startDate && d.Date <= endDate)
                .Select(d => new
                {
                    date = d.Date.ToString("yyyy-MM-dd"),
                    startTime = d.StartTime,
                    endTime = d.EndTime,
                    isUnavailable = d.IsUnavailable
                })
                .ToListAsync();

            return new JsonResult(dispositions);
        }
    }
}