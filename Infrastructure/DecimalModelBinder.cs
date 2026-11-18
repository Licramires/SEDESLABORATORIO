using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SEDESLABORATORIO.Infrastructure;

// Los inputs HTML number envían punto decimal, independientemente del idioma de Windows.
public sealed class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        var raw = value.FirstValue;
        if (string.IsNullOrWhiteSpace(raw) && Nullable.GetUnderlyingType(context.ModelType) != null)
            context.Result = ModelBindingResult.Success(null);
        else if (decimal.TryParse(raw?.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
            context.Result = ModelBindingResult.Success(number);
        else context.ModelState.TryAddModelError(context.ModelName, "Ingrese un número decimal válido, sin separadores de miles.");
        return Task.CompletedTask;
    }
}

public sealed class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        (Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType) == typeof(decimal) ? new DecimalModelBinder() : null;
}
