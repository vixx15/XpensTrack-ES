using System.Text.Json.Serialization;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Domain.Events;

public sealed record WalletCreated(
    Guid WalletId,
    string Name,
    Money OpeningBalance,
    [property: JsonPropertyName("walletTypeId")] WalletType WalletType,
    string UserId,
    decimal DefaultCurrencyExchangeRate,
    Money DefaultCurrencyOpeningBalance
);
