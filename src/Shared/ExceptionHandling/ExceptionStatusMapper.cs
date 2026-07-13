namespace Shared.ExceptionHandling;

public static class ExceptionStatusMapper
{
    public static int MapToStatusCode(Exception exception) => exception switch
    {
        KeyNotFoundException => 404,
        UnauthorizedAccessException => 403,
        ArgumentException => 400,
        InvalidOperationException => 400,
        _ => 500
    };
}
