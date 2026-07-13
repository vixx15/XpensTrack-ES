using MediatR;
using TransactionApi.Domain;

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
    Guid? ToWalletId,
    string? ToWalletCurrencyId) : IRequest<Guid>;