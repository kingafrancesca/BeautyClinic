  # BeautyClinic

  A booking system for beauty clinics. Clients can browse services and book appointments with a specific employee. 
  Employees get their own dashboard for managing bookings and availability.

  ## Stack

  - ASP.NET Core 9, Razor Pages
  - Entity Framework Core, SQL Server
  - ASP.NET Identity
  - HTML/CSS/JS

  ## Features

  Clients can register, browse the service catalog, book an appointment with a chosen employee, and view their
  booking history. They can also edit their account or delete it entirely.

  Employees have a separate view with their assigned bookings and a disposition panel for setting availability.

  ## Running locally

  1. Clone the repo
  2. Copy `appsettings.Example.json` to `appsettings.json` and fill in your SQL Server credentials
  3. `dotnet ef database update`
  4. `dotnet run`

  ## Roles

  Two roles are seeded automatically on startup: `Klient` (client) and `Pracownik` (employee). Assign them
  directly in the database or via a SQL script.

  ## Author
  Built by Kinga Kinowska.
