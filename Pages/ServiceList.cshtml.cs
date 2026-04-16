using BeautyClinic.Data;
using BeautyClinic.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeautyClinic.Pages
{
    public class ServiceListModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ServiceListModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Service> Services { get; set; }

        public void OnGet()
        {
            Services = _context.Services.ToList();
        }
    }
}