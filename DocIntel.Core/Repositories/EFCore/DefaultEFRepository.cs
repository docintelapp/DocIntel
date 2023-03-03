/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DocIntel.Core.Authorization;
using Ganss.Xss;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    public abstract class DefaultEFRepository<T> where T : class
    {
        protected readonly IAppAuthorizationService _appAuthorizationService;
        protected readonly IPublishEndpoint _busClient;
        protected readonly HtmlSanitizer _sanitizer;
        protected readonly Func<AmbientContext, DbSet<T>> _tableSelector;

        protected DefaultEFRepository(Func<AmbientContext, DbSet<T>> tableSelector, IPublishEndpoint busClient, IAppAuthorizationService appAuthorizationService)
        {
            _appAuthorizationService = appAuthorizationService;
            _busClient = busClient;
            _tableSelector = tableSelector;
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedSchemes.Add("data");
        }

        protected static string GenerateSlug(string str, int i = 0)
        {
            return Regex.Replace(Regex.Replace(str, @"[^A-Za-z0-9_\.~]+", "-"), "-{2,}", "-")
                .ToLowerInvariant().Trim('-') + (i > 0 ? "-"+i : "") ;
        }

        protected void PublishMessage(AmbientContext ambientContext, object o)
        {
            ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(() => _busClient.Publish(o));
        }

        protected bool IsValid(T data, List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(data);
            var isValid = Validator.TryValidateObject(data,
                validationContext,
                modelErrors);
            return isValid;
        }
    }
}