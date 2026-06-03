using MediatR;
using WalletApi.Projections;

namespace WalletApi.Application.Query;

public record GetAllWalletsOverview(string UserId) : IRequest<AllWalletsOverview?>;
