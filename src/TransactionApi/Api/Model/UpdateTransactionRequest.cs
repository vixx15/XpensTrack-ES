using Shared;

namespace TransactionApi.Api.Model;

public record UpdateTransactionRequest(
    WalletRequest Wallet,
    decimal Amount,
    TransactionType TransactionType,
    int? TransactionSubCategory,
    string Description,
    DateTimeOffset OccuredAt,
    TransferWalletRequest? TransferDetails
);