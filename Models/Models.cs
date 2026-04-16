using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BeautyClinic.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public new string PhoneNumber { get; set; } = null!;
    }

    public class Booking
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime Date { get; set; }
        [DisplayFormat(DataFormatString = @"hh\:mm\:ss", ApplyFormatInEditMode = true)]
        public TimeSpan Time { get; set; }
        public string? EmployeeId { get; set; }
        public Service? Service { get; set; }
        public ApplicationUser? Employee { get; set; }
    }

    public class Disposition
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsUnavailable { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ApplicationUser Employee { get; set; } = null!;
    }
}