using MediatR;
using UserManagement.Api.Model;

namespace UserManagement.Application.Command;

public record LoginUser(
    string Email,
    string Password
) : IRequest<AuthResponse>;
