/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DocIntel.Core.Exceptions
{
    public class InvalidArgumentException : DocIntelException
    {
        public InvalidArgumentException()
            : base("The argument is not valid.")
        {
            Errors = new Dictionary<string, List<string>>();
        }

        public InvalidArgumentException(IList<ValidationResult> validationResults)
            : base("The argument is not valid.")
        {
            Errors = new Dictionary<string, List<string>>();
            foreach (var result in validationResults)
            foreach (var field in result.MemberNames)
            {
                if (!Errors.ContainsKey(field)) Errors.Add(field, new List<string>());
                Errors[field].Add(result.ErrorMessage);
            }
        }

        public InvalidArgumentException(ModelStateDictionary modelStateDictionary)
            : base("The argument is not valid.")
        {
            Errors = new Dictionary<string, List<string>>();
            foreach (var kv in modelStateDictionary)
            {
                var ms = kv.Value;
                var field = kv.Key;

                if (!Errors.ContainsKey(field)) Errors.Add(field, new List<string>());

                foreach (var error in ms.Errors)
                    Errors[field].Add(string.IsNullOrEmpty(error.ErrorMessage)
                        ? error.Exception.Message
                        : error.ErrorMessage);
            }
        }

        public Dictionary<string, List<string>> Errors { get; set; }
    }
}