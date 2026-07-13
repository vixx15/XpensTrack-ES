namespace TransactionApi.Infrastructure.Inbox;

public record InboxRecord(Guid Id, DateTimeOffset ProcessedAt);
