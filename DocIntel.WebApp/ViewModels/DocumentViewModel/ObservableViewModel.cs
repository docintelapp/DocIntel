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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

using DocIntel.Core.Models;
using DocIntel.Core.Utils.Observables;

namespace DocIntel.WebApp.ViewModels.DocumentViewModel
{
    public class ObservableViewModel : IValidatableObject
    {
        // TODO Use DataAnnotation
        // TODO Reuse the same regex in the extraction code
        private const string regex_domain =
            @"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]$";

        private const string regex_url =
            @"^(http)?s?:?(//)?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\s\+.~#?&//=]*)$";

        private const string regex_ipv4 =
            @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$";

        private const string MD5_RE = @"^([a-fA-F\d]{32})$";
        private const string SHA1_RE = @"^([a-fA-F\d]{40})$";
        private const string SHA256_RE = @"^([a-fA-F\d]{64})$";
        private const string SHA512_RE = @"^([a-fA-F\d]{128})$";

        public bool IsAccepted { get; set; }
        public bool IsWhitelisted { get; set; }
        
        public ObservableStatus History { get; set; }

        [Required] public Guid Id { get; set; }

        public ObservableType Type { get; set; }

        public string Value { get; set; }

        public ObservableStatus Status { get; set; }
        public IList<ObservableHash> Hashes { get; set; }

        public IList<Tag> Tags { get; set; }

        public ObservableHashType HashType { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Value))
                yield break;
            
            Regex r = null;
            switch (Type)
            {
                case ObservableType.IPv4:
                    r = new Regex(regex_ipv4);
                    if (!r.IsMatch(Value))
                        yield return new ValidationResult(
                            $"Observable IP value is invalid {Value}.",
                            new[] {nameof(Value)});
                    break;
                case ObservableType.FQDN:
                    r = new Regex(regex_domain);
                    if (!r.IsMatch(Value))
                        yield return new ValidationResult(
                            $"Observable domain value is invalid {Value}.",
                            new[] {nameof(Value)});
                    break;
                case ObservableType.URL:
                    r = new Regex(regex_url);
                    if (!r.IsMatch(Value))
                        yield return new ValidationResult(
                            $"Observable url value is invalid {Value}.",
                            new[] {nameof(Value)});
                    break;
                case ObservableType.File:
                case ObservableType.Artefact:

                    switch (HashType)
                    {
                        case ObservableHashType.MD5:
                            r = new Regex(MD5_RE);
                            break;
                        case ObservableHashType.SHA1:
                            r = new Regex(SHA1_RE);
                            break;
                        case ObservableHashType.SHA256:
                            r = new Regex(SHA256_RE);
                            break;
                        case ObservableHashType.SHA512:
                            r = new Regex(SHA512_RE);
                            break;
                    }

                    if (r is not null)
                    {
                        // first hash value is mapped to value attribute
                        if (!r.IsMatch(Value))
                            yield return new ValidationResult(
                                $"Observable hash value is invalid {Value}.",
                                new[] {nameof(Value)});
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}