using System.Text.Json.Serialization;
using Shared;

namespace WalletApi.Domain.Events;

public sealed record WalletTypeChanged(
    Guid WalletId,
    string UserId,
    [property: JsonPropertyName("newTypeId")] WalletType NewType,
    [property: JsonPropertyName("walletTypeId")] WalletType OldType
);
