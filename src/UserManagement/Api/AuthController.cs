using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Api.Model;
using UserManagement.Application.Command;

namespace UserManagement.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterUser(
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email,
            Password: request.Password,
            DefaultCurrencyCode: request.DefaultCurrencyCode,
            OtherCurrencies: request.OtherCurrencies
        );

        var result = await mediator.Send(command);

        return CreatedAtAction(nameof(Register), result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginUser(
            Email: request.Email,
            Password: request.Password
        );

        var result = await mediator.Send(command);

        return Ok(result);
    }

}
