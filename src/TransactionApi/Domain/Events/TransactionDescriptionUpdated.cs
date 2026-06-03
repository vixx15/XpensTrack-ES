namespace TransactionApi.Domain.Events;

public sealed record TransactionDescriptionUpdated(
    Guid TransactionId,
    string Description
);