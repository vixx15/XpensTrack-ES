namespace WalletApi.Domain.Events;

public sealed record WalletTypeChanged(
    Guid WalletId,
    string UserId,
    long NewTypeId,
    long WalletTypeId
);