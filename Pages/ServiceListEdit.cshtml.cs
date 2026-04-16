using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BeautyClinic.Data;
using BeautyClinic.Models;

namespace BeautyClinic.Pages
{
    public class ServiceListEditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceListEditModel> _logger;

        public ServiceListEditModel(ApplicationDbContext context, ILogger<ServiceListEditModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Service> Services { get; set; } = new List<Service>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Services = await _context.Services
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Wystąpił błąd serwera podczas ładowania listy usług.", details = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync([FromForm] Service service)
        {
            try
            {
                if (service.Id <= 0)
                {
                    return new JsonResult(new { error = "Nieprawidłowy identyfikator usługi" }) { StatusCode = 400 };
                }

                if (string.IsNullOrWhiteSpace(service.Name))
                {
                    return new JsonResult(new { error = "Nazwa usługi jest wymagana" }) { StatusCode = 400 };
                }

                if (service.Price < 0)
                {
                    return new JsonResult(new { error = "Cena nie może być ujemna" }) { StatusCode = 400 };
                }

                if (service.DurationMinutes <= 0)
                {
                    return new JsonResult(new { error = "Czas trwania musi być większy od 0" }) { StatusCode = 400 };
                }

                var existingService = await _context.Services.FindAsync(service.Id);
                if (existingService == null)
                {
                    return new JsonResult(new { error = $"Usługa o ID {service.Id} nie istnieje" }) { StatusCode = 404 };
                }

                existingService.Name = service.Name;
                existingService.Description = string.IsNullOrEmpty(service.Description) ? null : service.Description;
                existingService.Price = service.Price;
                existingService.DurationMinutes = service.DurationMinutes;

                try
                {
                    await _context.SaveChangesAsync();
                    return new JsonResult(new { redirectUrl = Url.Page("/ServiceListEdit") }) { StatusCode = 200 };
                }
                catch (DbUpdateException dbEx)
                {
                    return new JsonResult(new { error = "Błąd podczas zapisywania zmian w usłudze.", details = dbEx.InnerException?.Message ?? dbEx.Message }) { StatusCode = 500 };
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = "Wystąpił błąd serwera podczas aktualizacji usługi", details = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostAddAsync([FromForm] Service service)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(service.Name))
                {
                    return new JsonResult(new { error = "Nazwa usługi jest wymagana" }) { StatusCode = 400 };
                }

                if (service.Price < 0)
                {
                    return new JsonResult(new { error = "Cena nie może być ujemna" }) { StatusCode = 400 };
                }

                if (service.DurationMinutes <= 0)
                {
                    return new JsonResult(new { error = "Czas trwania musi być większy od 0" }) { StatusCode = 400 };
                }

                service.Id = 0;
                service.Description = string.IsNullOrEmpty(service.Description) ? null : service.Description;

                _context.Services.Add(service);

                try
                {
                    await _context.SaveChangesAsync();
                    return new JsonResult(new { redirectUrl = Url.Page("/ServiceListEdit") }) { StatusCode = 200 };
                }
                catch (DbUpdateException dbEx)
                {
                    return new JsonResult(new { error = "Błąd podczas zapisywania nowej usługi.", details = dbEx.InnerException?.Message ?? dbEx.Message }) { StatusCode = 500 };
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = "Wystąpił błąd serwera podczas dodawania usługi", details = ex.Message }) { StatusCode = 500 };
            }
        }
    }
}