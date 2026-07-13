using MassTransit;
using MediatR;
using Shared.Contracts;
using WalletApi.Application.Command;
using WalletApi.Domain;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Infrastructure.Consumers;

public class TransactionDeletedConsumer(IMediator mediator) : IConsumer<TransactionDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TransactionDeletedIntegrationEvent> context)
    {
        var msg = context.Message;
        var messageId = context.MessageId
            ?? throw new InvalidOperationException("Message is missing MessageId, cannot guarantee idempotency.");

        await mediator.Send(new RevertTransactionOnWallet(
            MessageId: messageId,
            TransactionId: msg.TransactionId,
            WalletId: msg.WalletId,
            UserId: msg.UserId,
            Amount: new Money(msg.Amount, msg.AmountCurrencyCode),
            DefaultCurrencyAmount: new Money(msg.DefaultCurrencyAmount, msg.DefaultCurrencyCode),
            OriginalDirection: ToDirection(msg.TransactionType),
            OccurredAt: msg.OccuredAt,
            Transfer: TransactionCreatedConsumer.BuildTransferTarget(
                msg.ToWalletId, msg.ToWalletAmount, msg.ToWalletAmountCurrencyCode,
                msg.ToWalletCurrencyExchangeRate, msg.ToWalletCurrencyCode)));
    }

    private static BalanceDirection ToDirection(string transactionType) => transactionType switch
    {
        "Income" => BalanceDirection.Added,
        "Expense" => BalanceDirection.Deducted,
        "Transfer" => BalanceDirection.Deducted,
        _ => throw new ArgumentOutOfRangeException(nameof(transactionType), transactionType, null)
    };
}
