using Microsoft.Extensions.Localization;
using TransactionApi.Domain;

namespace TransactionApi.Infrastructure.Localization;

public class TransactionDisplayNames
{
    private readonly IStringLocalizer _localizer;

    public TransactionDisplayNames(IStringLocalizerFactory factory)
    {
        _localizer = factory.Create("TransactionApi.Resources.Resources", "TransactionApi");
    }

    public string GetCategoryName(TransactionCategory category)
    {
        var value = _localizer[$"Category_{category}"];
        return value.ResourceNotFound ? category.ToString() : value.Value;
    }

    public string GetTypeName(TransactionType type)
    {
        var value = _localizer[$"TransactionType_{type}"];
        return value.ResourceNotFound ? type.ToString() : value.Value;
    }
}
