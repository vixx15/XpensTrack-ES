namespace Shared.Contracts;

public record WalletCreatedIntegrationEvent(
    Guid WalletId,
    string Name,
    string CurrencyCode,
    string WalletType,
    string UserId);
