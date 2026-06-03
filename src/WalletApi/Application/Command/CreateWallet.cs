using MediatR;

namespace WalletApi.Application.Command;

public record CreateWallet(
    string Name,
    decimal Amount,
    string CurrencyCode,
    long WalletTypeId,
    string UserId,
    string DefaultCurrencyCode,
    decimal DefaultCurrencyExchangeRate) : IRequest<Guid>;