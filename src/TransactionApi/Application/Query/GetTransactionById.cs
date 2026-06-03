using MediatR;
using TransactionApi.Projections;

namespace TransactionApi.Application.Query;

public record GetTransactionById(Guid Id) : IRequest<TransactionReadModel?>;