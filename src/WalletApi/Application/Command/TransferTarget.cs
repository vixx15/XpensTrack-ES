using WalletApi.Domain.ValueObjects;

namespace WalletApi.Application.Command;

public record TransferTarget(Guid WalletId, Money Amount, Money DefaultCurrencyAmount);