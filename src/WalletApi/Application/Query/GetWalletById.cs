using MediatR;
using WalletApi.Projections;

namespace WalletApi.Application.Query;

public record GetWalletById(Guid Id, string UserId) : IRequest<WalletSummary?>;