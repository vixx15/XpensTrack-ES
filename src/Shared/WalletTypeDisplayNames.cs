using Microsoft.Extensions.Localization;

namespace Shared;

public class WalletTypeDisplayNames
{
    private readonly IStringLocalizer _localizer;

    public WalletTypeDisplayNames(IStringLocalizerFactory factory)
    {
        _localizer = factory.Create("Shared.Resources.Resources", "Shared");
    }

    public string GetWalletTypeName(WalletType type)
    {
        var value = _localizer[$"WalletType_{type}"];
        return value.ResourceNotFound ? type.ToString() : value.Value;
    }
}
