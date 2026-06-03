using MediatR;

namespace WalletApi.Application.Command;

public record UpdateWallet(Guid WalletId, string UserId, string NewName, long NewTypeId) : IRequest;