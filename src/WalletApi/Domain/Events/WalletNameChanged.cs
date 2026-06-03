namespace WalletApi.Domain.Events;

public sealed record WalletNameChanged(
    Guid WalletId,
    string UserId,
    string NewName
);