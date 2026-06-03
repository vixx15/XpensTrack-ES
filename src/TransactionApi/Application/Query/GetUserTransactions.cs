using MediatR;
using TransactionApi.Projections;

namespace TransactionApi.Application.Query;

public record GetUserTransactions(string UserId) : IRequest<IReadOnlyList<TransactionReadModel>>;