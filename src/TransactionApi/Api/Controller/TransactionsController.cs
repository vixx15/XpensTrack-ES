using MediatR;
using Microsoft.AspNetCore.Mvc;
using TransactionApi.Api.Model;
using TransactionApi.Application.Command;
using TransactionApi.Application.Query;

namespace TransactionApi.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        // var userId= User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var command = new CreateTransaction(
            WalletId: request.Wallet.WalletId,
            "123", //todo user
            Amount: request.Amount,
            TransactionType: request.TransactionType,
            TransactionSubCategory: request.TransactionSubCategory,
            Description: request.Description,
            OccuredAt: request.OccuredAt,
            CurrencyCode: request.Wallet.CurrencyCode,
            "RSD", //TODO reading user defaultCurrency logic
            DefaultCurrencyExchangeRate: 1.0M, //TODO exchangeRate logic
            ToWalletId: request.TransferWallet?.ToWalletId,
            ToWalletCurrencyId: request.TransferWallet?.ToWalletCurrencyCode,
            ToWalletCurrencyExchangeRate: request.TransferWallet != null ? 1.0M : null
        );

        var resultId = await mediator.Send(request: command);

        return CreatedAtAction(actionName: nameof(GetById), routeValues: new { id = resultId }, value: resultId);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDescription(Guid id, [FromBody] UpdateTransactionRequest request)
    {
        var transactionTransferTarget = request.TransferDetails is not null
            ? new UpdateTransactionTransferWallet(ToWalletId: request.TransferDetails.ToWalletId,
                ToWalletCurrencyCode: request.TransferDetails.ToWalletCurrencyCode,
                ToWalletExchangeRate: 1.0M)
            : null;

        await mediator.Send(request: new UpdateTransaction(
            TransactionId: id,
            WalletId: request.Wallet.WalletId,
            Amount: request.Amount,
            TransactionType: request.TransactionType,
            TransactionCategoryId: request.TransactionSubCategory,
            Description: request.Description,
            OccurredAt: request.OccuredAt,
            CurrencyCode: request.Wallet.CurrencyCode,
            TransferWallet: transactionTransferTarget,
            DefaultCurrencyExchangeRate: 1.0M, //TODO
            DefaultCurrencyCode: "RSD"
        ));
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        await mediator.Send(request: new DeleteTransaction(id, "123"));
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(request: new GetTransactionById(Id: id));

        return result is not null ? Ok(value: result) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var results = await mediator.Send(request: new GetUserTransactions("123"));

        return Ok(value: results);
    }

    [HttpGet("reports/{month:int}/{year:int}")]
    public async Task<IActionResult> GetReport(int month, int year)
    {
        var result = await mediator.Send(request: new GetMonthlyReportById(Id: $"{year}-{month:00}"));
        return result is not null ? Ok(value: result) : NotFound();
    }
}