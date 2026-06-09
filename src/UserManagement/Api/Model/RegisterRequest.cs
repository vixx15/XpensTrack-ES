using System.ComponentModel.DataAnnotations;

namespace UserManagement.Api.Model;

public record RegisterRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required] string Email,
    [Required] string Password,
    [Required] string DefaultCurrencyCode,
    List<string>? OtherCurrencies
);
