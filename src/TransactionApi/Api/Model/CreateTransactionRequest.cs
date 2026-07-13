using TransactionApi.Domain;

namespace TransactionApi.Api.Model;

public record CreateTransactionRequest(
    WalletRequest Wallet,
    decimal Amount,
    TransactionType TransactionType,
    TransactionCategory? Category,
    string Description,
    DateTimeOffset OccuredAt,
    TransferWalletRequest? TransferWallet
);