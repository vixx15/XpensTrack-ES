namespace UserManagement.Api.Model;

public record AuthResponse(
    string Token,
    string Email,
    string FirstName,
    string LastName
);
