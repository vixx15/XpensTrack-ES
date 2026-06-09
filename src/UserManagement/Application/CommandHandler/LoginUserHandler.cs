using MediatR;
using Microsoft.AspNetCore.Identity;
using UserManagement.Api.Model;
using UserManagement.Application.Command;
using UserManagement.Data;
using UserManagement.Services;

namespace UserManagement.Application.CommandHandler;

public class LoginUserHandler(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IJwtService jwtService) : IRequestHandler<LoginUser, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginUser command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, command.Password, false);
        if (!signInResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
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
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName
        );
    }
}