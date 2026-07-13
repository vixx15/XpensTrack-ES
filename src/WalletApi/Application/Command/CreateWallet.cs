using MediatR;
using WalletApi.Domain;

namespace WalletApi.Application.Command;

public record CreateWallet(
    string Name,
    decimal OpeningBalance,
    string CurrencyCode,
    WalletType WalletType,
    string UserId,
    string DefaultCurrencyCode) : IRequest<Guid>;
