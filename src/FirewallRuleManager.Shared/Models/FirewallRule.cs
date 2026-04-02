using System.ComponentModel.DataAnnotations;

namespace FirewallRuleManager.Shared.Models;

public class FirewallRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "From Hostname is required.")]
    [MaxLength(255, ErrorMessage = "From Hostname cannot exceed 255 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$", ErrorMessage = "From Hostname contains invalid characters. Only alphanumeric characters, hyphens, dots, and underscores are allowed.")]
    public string FromHostname { get; set; } = string.Empty;

    [Required(ErrorMessage = "To Hostname is required.")]
    [MaxLength(255, ErrorMessage = "To Hostname cannot exceed 255 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$", ErrorMessage = "To Hostname contains invalid characters. Only alphanumeric characters, hyphens, dots, and underscores are allowed.")]
    public string ToHostname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Port Number is required.")]
    [Range(1, 65535, ErrorMessage = "Port Number must be between 1 and 65535.")]
    public int PortNumber { get; set; }

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Protocol is required.")]
    [RegularExpression(@"^(TCP|UDP|ICMP|ANY)$", ErrorMessage = "Protocol must be one of: TCP, UDP, ICMP, ANY.")]
    public string Protocol { get; set; } = "TCP";

    [Required(ErrorMessage = "Registration Date is required.")]
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
}
