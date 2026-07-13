using MediatR;
using WalletApi.Domain;

namespace WalletApi.Application.Command;

public record UpdateWallet(Guid WalletId, string UserId, string NewName, WalletType NewType) : IRequest;
