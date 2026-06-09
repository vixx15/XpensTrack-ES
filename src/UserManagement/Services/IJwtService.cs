namespace UserManagement.Services;

public interface IJwtService
{
    string GenerateToken(string userId, string email, string firstName, string lastName,
        string defaultCurrencyCode, List<string> secondaryCurrencies);
}
