using System.Text.Json.Serialization;
using Shared;

namespace WalletApi.Domain.Events;

public sealed record WalletCreated(
    Guid WalletId,
    string Name,
    Money Amount,
    [property: JsonPropertyName("walletTypeId")] WalletType WalletType,
    string UserId,
    decimal DefaultCurrencyExchangeRate,
    Money DefaultCurrencyAmount
);
