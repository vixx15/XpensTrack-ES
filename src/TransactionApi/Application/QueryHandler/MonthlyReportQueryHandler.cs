using Marten;
using MediatR;
using TransactionApi.Application.Query;
using TransactionApi.Projections;

namespace TransactionApi.Application.QueryHandler;

public class MonthlyReportQueryHandler(IQuerySession session) :
    IRequestHandler<GetMonthlyReportById, MonthlyReport?>
{
    public async Task<MonthlyReport?> Handle(GetMonthlyReportById query, CancellationToken cancellationToken)
    {
        return await session.LoadAsync<MonthlyReport>(
            id: query.Id,
            token: cancellationToken);
    }
}