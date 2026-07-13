using CurrencyApi.Application.Query;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using XpensTrack.CurrencyApi.Api.Grpc;

namespace CurrencyApi.Api;

public class ExchangeRateGrpcService : ExchangeRateRpc.ExchangeRateRpcBase
{
    private readonly IMediator _mediator;

    public ExchangeRateGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<GetCurrentRateResponse> GetCurrentRate(GetCurrentRateRequest request,
        ServerCallContext context)
    {
        var query = new GetCurrentExchangeRateQuery(request.From, request.To);
        var rate = await _mediator.Send(query);

        if (rate == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Exchange rate from {request.From} to {request.To} not found."));
        }

        return new GetCurrentRateResponse { Rate = (double)rate.Rate, From = request.From, To = request.To };
    }

    public override async Task<GetHistoricalRateResponse> GetHistoricalRate(GetHistoricalRateRequest request,
        ServerCallContext context)
    {
        var query = new GetHistoricalExchangeRateQuery(request.From, request.To, request.Date.ToDateTime());
        var rate = await _mediator.Send(query);

        if (rate == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Historical exchange rate from {request.From} to {request.To} not found."));
        }

        return new GetHistoricalRateResponse
            { Rate = (double)rate.Rate, From = request.From, To = request.To, Date = rate.Date.ToTimestamp() };
    }
}