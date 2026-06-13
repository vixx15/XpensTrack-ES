using MediatR;
using Shared;

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
    decimal DefaultCurrencyExchangeRate,
    UpdateTransactionTransferWallet? TransferWallet) : IRequest;

public record UpdateTransactionTransferWallet(
    Guid ToWalletId,
    string ToWalletCurrencyCode,
    decimal ToWalletExchangeRate);