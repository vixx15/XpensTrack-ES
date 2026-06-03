using MediatR;
using Microsoft.AspNetCore.Mvc;
using WalletApi.Api.Model;
using WalletApi.Application.Command;
using WalletApi.Application.Query;

namespace WalletApi.Api;

[ApiController]
[Route("api/[controller]")]
public class WalletController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWalletRequest request)
    {
        // var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var command = new CreateWallet(
            Name: request.Name,
            Amount: request.Amount,
            CurrencyCode: request.CurrencyCode,
            WalletTypeId: request.WalletTypeId,
            "123", //todo user
            "RSD",
            DefaultCurrencyExchangeRate: 1.0M);

        var resultId = await mediator.Send(request: command);

        return CreatedAtAction(actionName: nameof(GetById), routeValues: new { id = resultId }, value: resultId);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(request: new GetWalletById(Id: id));

        return result is not null ? Ok(value: result) : NotFound();
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetAllWalletsOverview(string userId)
    {
        var result = await mediator.Send(new GetAllWalletsOverview(userId));

        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWalletRequest request)
    {
        await mediator.Send(request: new UpdateWallet(
            WalletId: id,
            UserId:"123",
            NewName: request.NewName,
            NewTypeId: request.NewTypeId));

        return NoContent();
    }
}