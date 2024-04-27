using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace honooru.Code {

    /// <summary>
    ///     a model binder that when given an empty string, will keep the empty string instead of converting it to null
    /// </summary>
    public class EmptyStringModelBinder : IModelBinder {

        private readonly ILogger<EmptyStringModelBinder> _Logger;

        public EmptyStringModelBinder(ILogger<EmptyStringModelBinder> logger) {
            _Logger = logger;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext) {
            ValueProviderResult result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (result == ValueProviderResult.None) {
                bindingContext.Result = ModelBindingResult.Success(null);
            } else if (result.Length == 1) {
                // handles "" staying as an empty string, instead of getting converted into null
                bindingContext.Result = ModelBindingResult.Success(result.FirstValue);
            }

            return Task.CompletedTask;
        }

    }

    public class EmptyStringModelBinderProvider : IModelBinderProvider {

        public IModelBinder? GetBinder(ModelBinderProviderContext context) {
            ILogger<EmptyStringModelBinder> logger = context.Services.CreateScope().ServiceProvider.GetRequiredService<ILogger<EmptyStringModelBinder>>();

            if (context.Metadata.ModelType == typeof(string)
                && context.BindingInfo.BindingSource != null
                && context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Query)) {

                return new EmptyStringModelBinder(logger);
            }

            return null;
        }

    }

}
