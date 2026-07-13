using MassTransit;
using MediatR;
using Shared.Contracts;
using WalletApi.Application.Command;
using WalletApi.Domain;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Infrastructure.Consumers;

public class TransactionUpdatedConsumer(IMediator mediator) : IConsumer<TransactionUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TransactionUpdatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var messageId = context.MessageId
            ?? throw new InvalidOperationException("Message is missing MessageId, cannot guarantee idempotency.");

        await mediator.Send(new AdjustTransactionOnWallet(
            MessageId: messageId,
            TransactionId: msg.TransactionId,
            UserId: msg.UserId,
            OldWalletId: msg.OldWalletId,
            OldAmount: new Money(msg.OldAmount, msg.OldAmountCurrencyCode),
            OldDefaultCurrencyAmount: new Money(msg.OldDefaultCurrencyAmount, msg.OldDefaultCurrencyCode),
            OldDirection: ToDirection(msg.OldTransactionType),
            OldOccurredAt: msg.OldOccuredAt,
            OldTransfer: TransactionCreatedConsumer.BuildTransferTarget(
                msg.OldToWalletId, msg.OldToWalletAmount, msg.OldToWalletAmountCurrencyCode,
                msg.OldToWalletCurrencyExchangeRate, msg.OldToWalletCurrencyCode),
            NewWalletId: msg.NewWalletId,
            NewAmount: new Money(msg.NewAmount, msg.NewAmountCurrencyCode),
            NewDefaultCurrencyAmount: new Money(msg.NewDefaultCurrencyAmount, msg.NewDefaultCurrencyCode),
            NewDirection: ToDirection(msg.NewTransactionType),
            NewOccurredAt: msg.NewOccurredAt,
            NewTransfer: TransactionCreatedConsumer.BuildTransferTarget(
                msg.NewToWalletId, msg.NewToWalletAmount, msg.NewToWalletAmountCurrencyCode,
                msg.NewToWalletCurrencyExchangeRate, msg.NewToWalletCurrencyCode)));
    }

    private static BalanceDirection ToDirection(string transactionType) => transactionType switch {
        "Income" => BalanceDirection.Added,
        "Expense" => BalanceDirection.Deducted,
        "Transfer" => BalanceDirection.Deducted,
        _ => throw new ArgumentOutOfRangeException(nameof(transactionType), transactionType, null)
    };
}