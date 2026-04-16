using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using BeautyClinic.Models;
using Microsoft.EntityFrameworkCore;

namespace BeautyClinic.Pages
{
    public class DeleteAccountModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly BeautyClinic.Data.ApplicationDbContext _context;

        public DeleteAccountModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            BeautyClinic.Data.ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Index");
            }

            var userBookings = await _context.Bookings.Where(b => b.UserId == user.Id).ToListAsync();
            if (userBookings.Any())
            {
                _context.Bookings.RemoveRange(userBookings);
                await _context.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();

                TempData["SuccessMessage"] = $"Użytkownik o ID {user.Id} poprawnie usunięty.";

                return RedirectToPage("/Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        public IActionResult OnPostCancel()
        {
            return RedirectToPage("/MyAccount");
        }
    }
}