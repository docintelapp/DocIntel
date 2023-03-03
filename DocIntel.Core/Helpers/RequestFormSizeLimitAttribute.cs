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
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DocIntel.Core.Helpers
{
    public class RequestFormSizeLimitAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
    {
        private readonly FormOptions _formOptions;

        public RequestFormSizeLimitAttribute(int valueCountLimit)
        {
            _formOptions = new FormOptions
            {
                ValueCountLimit = valueCountLimit
            };
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var features = context.HttpContext.Features;
            var formFeature = features.Get<IFormFeature>();
            if (formFeature == null || formFeature.Form == null)
                features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request,
                    _formOptions));
        }

        public int Order { get; set; }
    }
}