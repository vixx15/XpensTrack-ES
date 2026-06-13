using Marten;
using MediatR;
using TransactionApi.Application.Query;
using TransactionApi.Projections;

namespace TransactionApi.Application.QueryHandler;

public class MonthlyReportQueryHandler(IQuerySession session, MonthlyReportMapper mapper) :
    IRequestHandler<GetMonthlyReportById, MonthlyReportResponse?>
{
    public async Task<MonthlyReportResponse?> Handle(GetMonthlyReportById query, CancellationToken cancellationToken)
    {
        var report = await session.LoadAsync<MonthlyReport>(
            id: query.Id,
            token: cancellationToken);

        return report is null ? null : mapper.ToResponse(report);
    }
}