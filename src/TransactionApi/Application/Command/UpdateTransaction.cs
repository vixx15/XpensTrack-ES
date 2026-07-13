using MediatR;
using TransactionApi.Domain;

namespace TransactionApi.Application.Command;

public record UpdateTransaction(
    Guid TransactionId,
    Guid WalletId,
    string UserId,
    decimal Amount,
    string CurrencyCode,
    TransactionType TransactionType,
    TransactionCategory? Category,
    string Description,
    DateTimeOffset OccurredAt,
    string DefaultCurrencyCode,
    UpdateTransactionTransferWallet? TransferWallet) : IRequest;

public record UpdateTransactionTransferWallet(
    Guid ToWalletId,
    string ToWalletCurrencyCode);