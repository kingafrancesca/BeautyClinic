using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeautyClinic.Models;
using System.ComponentModel.DataAnnotations;

namespace BeautyClinic.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string SuccessMessage { get; set; } = null!;

        [TempData]
        public string ErrorMessage { get; set; } = null!;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            public string LastName { get; set; } = string.Empty;

            [Required]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "Wybierz rolę.")]
            public string Role { get; set; } = "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                PhoneNumber = Input.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (!result.Succeeded)
            {
                ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                return Page();
            }

           if (!await _roleManager.RoleExistsAsync("Klient"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Klient"));
            }
            if (!await _roleManager.RoleExistsAsync("Pracownik"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Pracownik"));
            }

            IdentityResult roleResult = null;
            if (Input.Role == "Klient")
            {
                roleResult = await _userManager.AddToRoleAsync(user, "Klient");
                SuccessMessage = "Przypisano rolę: Klient";
            }
            else if (Input.Role == "Pracownik")
            {
                roleResult = await _userManager.AddToRoleAsync(user, "Pracownik");
                SuccessMessage = "Przypisano rolę: Pracownik";
            }

            if (roleResult != null)
            {
                if (roleResult.Succeeded)
                {
                    await _userManager.UpdateAsync(user);
                    var roles = await _userManager.GetRolesAsync(user);
                    SuccessMessage += $"<br>Zweryfikowane role w bazie: {string.Join(", ", roles)}";
                }
                else
                {
                    ErrorMessage = $"Błąd przypisania roli: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}";
                }
            }
            else
            {
                ErrorMessage = "Nie wybrano roli lub wystąpił błąd.";
            }

            return RedirectToPage("/Index");
        }
    }
}