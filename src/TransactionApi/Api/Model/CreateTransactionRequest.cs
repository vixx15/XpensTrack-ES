using Shared;

namespace TransactionApi.Api.Model;

public record CreateTransactionRequest(
    WalletRequest Wallet,
    decimal Amount,
    TransactionType TransactionType,
    int? TransactionSubCategory,
    string Description,
    DateTimeOffset OccuredAt,
    TransferWalletRequest? TransferWallet
);