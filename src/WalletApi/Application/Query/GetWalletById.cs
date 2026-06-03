using MediatR;
using WalletApi.Projections;

namespace WalletApi.Application.Query;

public record GetWalletById(Guid Id) : IRequest<WalletSummary?>;