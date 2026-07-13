namespace Shared.Contracts;

public record WalletUpdatedIntegrationEvent(
    Guid WalletId,
    string UserId,
    string Name,
    string WalletType);
