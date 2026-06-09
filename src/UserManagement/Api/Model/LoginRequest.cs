using System.ComponentModel.DataAnnotations;

namespace UserManagement.Api.Model;

public record LoginRequest(
    [Required] string Email,
    [Required] string Password
);
