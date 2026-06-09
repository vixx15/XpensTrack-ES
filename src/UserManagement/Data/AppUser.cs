using Microsoft.AspNetCore.Identity;

namespace UserManagement.Data;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DefaultCurrencyCode { get; set; } = string.Empty;
    public List<string> OtherCurrencies { get; set; } = [];
}
