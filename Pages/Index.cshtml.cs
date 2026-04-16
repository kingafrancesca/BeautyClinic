using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeautyClinic.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }
    [TempData]
        public string SuccessMessage { get; set; } = null!;

    [TempData]
    public string ErrorMessage { get; set; } = null!;
    
    public void OnGet()
    {

    }
}
