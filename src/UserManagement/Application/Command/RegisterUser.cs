using MediatR;
using UserManagement.Api.Model;

namespace UserManagement.Application.Command;

public record RegisterUser(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string DefaultCurrencyCode,
    List<string>? OtherCurrencies
) : IRequest<AuthResponse>;
