using System.ComponentModel.DataAnnotations;

namespace FirewallRuleManager.Shared.Models;

public class FirewallRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "From Hostname is required.")]
    [MaxLength(255, ErrorMessage = "From Hostname cannot exceed 255 characters.")]
    public string FromHostname { get; set; } = string.Empty;

    [Required(ErrorMessage = "To Hostname is required.")]
    [MaxLength(255, ErrorMessage = "To Hostname cannot exceed 255 characters.")]
    public string ToHostname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Port Number is required.")]
    [Range(1, 65535, ErrorMessage = "Port Number must be between 1 and 65535.")]
    public int PortNumber { get; set; }

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Protocol is required.")]
    public string Protocol { get; set; } = "TCP";

    [Required(ErrorMessage = "Registration Date is required.")]
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
}
