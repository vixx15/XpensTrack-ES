using Microsoft.Extensions.Localization;
using WalletApi.Domain;

namespace WalletApi.Infrastructure.Localization;

public class WalletTypeDisplayNames
{
    private readonly IStringLocalizer _localizer;

    public WalletTypeDisplayNames(IStringLocalizerFactory factory)
    {
        _localizer = factory.Create("WalletApi.Resources.Resources", "WalletApi");
    }

    public string GetWalletTypeName(WalletType type)
    {
        var value = _localizer[$"WalletType_{type}"];
        return value.ResourceNotFound ? type.ToString() : value.Value;
    }
}
