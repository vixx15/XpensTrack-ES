using MediatR;
using Microsoft.AspNetCore.Identity;
using UserManagement.Api.Model;
using UserManagement.Application.Command;
using UserManagement.Data;
using UserManagement.Services;

namespace UserManagement.Application.CommandHandler;

public class RegisterUserHandler(
    UserManager<AppUser> userManager,
    IJwtService jwtService) : IRequestHandler<RegisterUser, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUser command, CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var user = new AppUser
        {
            UserName = command.Email,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            DefaultCurrencyCode = command.DefaultCurrencyCode,
            OtherCurrencies = command.OtherCurrencies ?? []
        };

        var result = await userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User creation failed: {errors}");
        }

        var token = jwtService.GenerateToken(
            userId: user.Id,
            email: user.Email!,
            firstName: user.FirstName,
            lastName: user.LastName,
            defaultCurrencyCode: user.DefaultCurrencyCode,
            secondaryCurrencies: user.OtherCurrencies
        );

        return new AuthResponse(
            Token: token,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName
        );
    }
}
