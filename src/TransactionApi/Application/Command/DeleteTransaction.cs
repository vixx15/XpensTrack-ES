using MediatR;

namespace TransactionApi.Application.Command;

public record DeleteTransaction(Guid TransactionId, string UserId): IRequest;