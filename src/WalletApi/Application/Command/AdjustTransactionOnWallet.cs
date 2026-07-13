using MediatR;
using WalletApi.Domain;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Application.Command;

public record AdjustTransactionOnWallet(
    Guid MessageId,
    Guid TransactionId,
    string UserId,
    Guid OldWalletId,
    Money OldAmount,
    Money OldDefaultCurrencyAmount,
    BalanceDirection OldDirection,
    DateTimeOffset OldOccurredAt,
    TransferTarget? OldTransfer,
    Guid NewWalletId,
    Money NewAmount,
    Money NewDefaultCurrencyAmount,
    BalanceDirection NewDirection,
    DateTimeOffset NewOccurredAt,
    TransferTarget? NewTransfer) : IRequest;
