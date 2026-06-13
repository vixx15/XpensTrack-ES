using MediatR;
using Shared;

namespace WalletApi.Application.Command;

public record CreateWallet(
    string Name,
    decimal Amount,
    string CurrencyCode,
    WalletType WalletType,
    string UserId,
    string DefaultCurrencyCode,
    decimal DefaultCurrencyExchangeRate) : IRequest<Guid>;
