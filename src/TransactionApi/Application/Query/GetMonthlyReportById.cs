using MediatR;
using TransactionApi.Projections;

namespace TransactionApi.Application.Query;

public record GetMonthlyReportById(string Id) : IRequest<MonthlyReportResponse?>;