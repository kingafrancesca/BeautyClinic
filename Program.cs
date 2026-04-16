using BeautyClinic.Data;
using BeautyClinic.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("RequireEmployeeRole", policy => policy.RequireRole("Pracownik"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/AccessDenied";
    options.LogoutPath = "/Logout";
    options.ReturnUrlParameter = "returnUrl";

    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            if (context.Request.Path != "/Login" && !context.HttpContext.User.Identity?.IsAuthenticated == true && !context.Response.HasStarted)
            {
                context.Response.Redirect("/Index");
            }
            else if (!context.Response.HasStarted)
            {
                context.Response.Redirect(context.RedirectUri);
            }
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect("/AccessDenied");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Klient", "Pracownik" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            try
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas tworzenia roli {role}: {ex.Message}");
            }
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();