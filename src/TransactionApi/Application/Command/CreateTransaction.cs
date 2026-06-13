using MediatR;
using Shared;

namespace TransactionApi.Application.Command;

public record CreateTransaction(
    Guid WalletId,
    string UserId,
    decimal Amount,
    TransactionType TransactionType,
    TransactionCategory? Category,
    string Description,
    DateTimeOffset OccuredAt,
    string CurrencyCode,
    string DefaultCurrencyCode,
    decimal DefaultCurrencyExchangeRate,
    Guid? ToWalletId,
    string? ToWalletCurrencyId,
    decimal? ToWalletCurrencyExchangeRate) : IRequest<Guid>;