using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SEDESLABORATORIO.Infrastructure;

// Una casilla alterada debe mostrar un error; Razor no puede representar texto arbitrario como bool.
public sealed class BooleanModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        if (bool.TryParse(value.FirstValue, out var parsed))
        {
            context.ModelState.SetModelValue(context.ModelName, value);
            context.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            context.ModelState.SetModelValue(context.ModelName, new ValueProviderResult("false"));
            context.ModelState.TryAddModelError(context.ModelName, "Seleccione una opción válida.");
            context.Result = ModelBindingResult.Success(false);
        }
        return Task.CompletedTask;
    }
}

public sealed class BooleanModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.ModelType == typeof(bool) ? new BooleanModelBinder() : null;
}
