using MassTransit;
using MediatR;
using Shared.Contracts;
using WalletApi.Application.Command;
using WalletApi.Domain;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Infrastructure.Consumers;

public class TransactionCreatedConsumer(IMediator mediator) : IConsumer<TransactionCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TransactionCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var messageId = context.MessageId
                        ?? throw new InvalidOperationException(
                            "Message is missing MessageId, cannot guarantee idempotency.");

        await mediator.Send(new ApplyTransactionToWallet(
            MessageId: messageId,
            TransactionId: msg.TransactionId,
            WalletId: msg.WalletId,
            UserId: msg.UserId,
            Amount: new Money(msg.Amount, msg.AmountCurrencyCode),
            DefaultCurrencyAmount: new Money(msg.DefaultCurrencyAmount, msg.DefaultCurrencyCode),
            Direction: ToDirection(msg.TransactionType),
            OccurredAt: msg.OccuredAt,
            Transfer: BuildTransferTarget(msg.ToWalletId, msg.ToWalletAmount, msg.ToWalletAmountCurrencyCode,
                msg.ToWalletCurrencyExchangeRate, msg.ToWalletCurrencyCode)));
    }

    private static BalanceDirection ToDirection(string transactionType) => transactionType switch {
        "Income" => BalanceDirection.Added,
        "Expense" => BalanceDirection.Deducted,
        "Transfer" => BalanceDirection.Deducted,
        _ => throw new ArgumentOutOfRangeException(nameof(transactionType), transactionType, null)
    };

    internal static TransferTarget? BuildTransferTarget(
        Guid? toWalletId, decimal? toWalletAmount, string? toWalletAmountCurrencyCode,
        decimal? defaultCurrencyAmount, string? defaultCurrencyCode)
    {
        if (toWalletId is null) return null;

        if (toWalletAmount is null || toWalletAmountCurrencyCode is null)
            throw new InvalidOperationException(
                $"Transfer transaction is missing ToWalletAmount for wallet '{toWalletId}'.");

        if (defaultCurrencyAmount is null || defaultCurrencyCode is null)
            throw new InvalidOperationException(
                $"Transfer transaction for wallet '{toWalletId}' is missing ToWalletCurrencyExchangeRate or ToWalletCurrencyCode.");

        var amount = new Money(toWalletAmount.Value, toWalletAmountCurrencyCode);
        var defaultCurrencyMoney = new Money(defaultCurrencyAmount.Value, defaultCurrencyCode);

        return new TransferTarget(toWalletId.Value, amount, defaultCurrencyMoney);
    }
}