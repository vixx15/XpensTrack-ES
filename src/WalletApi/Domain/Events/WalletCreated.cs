using Shared;

namespace WalletApi.Domain.Events;

public sealed record WalletCreated(
    Guid WalletId,
    string Name,
    Money Amount,
    long WalletTypeId,
    string UserId,
    decimal DefaultCurrencyExchangeRate,
    Money DefaultCurrencyAmount
);