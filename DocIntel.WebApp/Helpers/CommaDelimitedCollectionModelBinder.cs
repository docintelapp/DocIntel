/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau, Kevin Menten
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DocIntel.WebApp.Helpers
{
    public class DelimitedArrayModelBinderProvider : IModelBinderProvider
    {
        private readonly IModelBinder modelBinder;

        public DelimitedArrayModelBinderProvider()
            : this(',')
        {
        }

        public DelimitedArrayModelBinderProvider(params char[] delimiters)
        {
            if (delimiters == null) throw new ArgumentNullException(nameof(delimiters));

            modelBinder = new DelimitedArrayModelBinder(delimiters);
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Metadata.IsEnumerableType
                && !context.Metadata.ElementMetadata.IsComplexType)
                return modelBinder;

            return null;
        }
    }

    public class DelimitedArrayModelBinder : IModelBinder
    {
        private readonly char[] _delimiters;

        public DelimitedArrayModelBinder(char[] delimiters)
        {
            _delimiters = delimiters;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            var values = valueProviderResult
                .ToString()
                .Split(_delimiters, StringSplitOptions.RemoveEmptyEntries);

            // TODO: Do not work with arrays. 
            var elementType = bindingContext.ModelType.GetTypeInfo().GenericTypeArguments[0];

            if (values.Length == 0)
            {
                bindingContext.Result = ModelBindingResult.Success(Array.CreateInstance(elementType, 0));
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(elementType);
                var typedArray = Array.CreateInstance(elementType, values.Length);

                try
                {
                    for (var i = 0; i < values.Length; ++i)
                    {
                        var value = values[i];
                        var convertedValue = converter.ConvertFromString(value);
                        typedArray.SetValue(convertedValue, i);
                    }
                }
                catch (Exception exception)
                {
                    bindingContext.ModelState.TryAddModelError(
                        modelName,
                        exception,
                        bindingContext.ModelMetadata);
                }

                bindingContext.Result = ModelBindingResult.Success(typedArray);
            }

            return Task.CompletedTask;
        }
    }
}