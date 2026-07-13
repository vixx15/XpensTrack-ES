using MediatR;
using WalletApi.Domain;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Application.Command;

public record RevertTransactionOnWallet(
    Guid MessageId,
    Guid TransactionId,
    Guid WalletId,
    string UserId,
    Money Amount,
    Money DefaultCurrencyAmount,
    BalanceDirection OriginalDirection,
    DateTimeOffset OccurredAt,
    TransferTarget? Transfer) : IRequest;
