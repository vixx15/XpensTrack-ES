namespace UserManagement.Application.Exceptions;

public class AuthenticationFailedException(string message) : Exception(message);
