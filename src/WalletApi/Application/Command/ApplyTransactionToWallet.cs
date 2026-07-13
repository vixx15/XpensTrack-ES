using MediatR;
using WalletApi.Domain;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Application.Command;

public record ApplyTransactionToWallet(
    Guid MessageId,
    Guid TransactionId,
    Guid WalletId,
    string UserId,
    Money Amount,
    Money DefaultCurrencyAmount,
    BalanceDirection Direction,
    DateTimeOffset OccurredAt,
    TransferTarget? Transfer) : IRequest;
