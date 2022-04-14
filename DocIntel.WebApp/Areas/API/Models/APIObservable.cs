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
using System.Text.RegularExpressions;

using DocIntel.Core.Helpers;
using DocIntel.Core.Models;

namespace DocIntel.WebApp.Areas.API.Models
{
    public class APIObservable : Observable, IValidatableObject
    {
        // TODO Re-use the same regexes as the one available in observable extraction
        // TODO Use DataAnnotation
        private const string REGEX_DOMAIN =
            @"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]$";

        // TODO Re-use the same regexes as the one available in observable extraction
        // TODO Use DataAnnotation
        private const string REGEX_URL =
            @"^(http)?s?:?(//)?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\s\+.~#?&//=]*)$";

        // TODO Re-use the same regexes as the one available in observable extraction
        // TODO Use DataAnnotation
        private const string REGEX_IPV4 =
            @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$";

        // TODO Re-use the same regexes as the one available in observable extraction
        // TODO Use DataAnnotation
        private const string MD5_RE = @"^([a-fA-F\d]{32})$";

        // TODO Re-use the same regexes as the one available in observable extraction
        // TODO Use DataAnnotation
        private const string SHA1_RE = @"^([a-fA-F\d]{40})$";

        // TODO Re-use the same regexes as the one available in observable extraction
        // TODO Use DataAnnotation
        private const string SHA256_RE = @"^([a-fA-F\d]{64})$";

        // TODO Re-use the same regexes as the one available in observable extraction
        // TODO Use DataAnnotation
        private const string SHA512_RE = @"^([a-fA-F\d]{128})$";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            Regex r = null;
            if (Value.IsNullOrEmpty() && Hashes is null)
                yield return new ValidationResult(
                    "Provide valid data",
                    new[] {nameof(Value)});

            switch (Type)
            {
                case ObservableType.IPv4:
                    r = new Regex(REGEX_IPV4);
                    if (!r.IsMatch(Value))
                        yield return new ValidationResult(
                            $"Observable IP value is invalid {Value}.",
                            new[] {nameof(Value)});
                    break;
                case ObservableType.FQDN:
                    r = new Regex(REGEX_DOMAIN);
                    if (!r.IsMatch(Value))
                        yield return new ValidationResult(
                            $"Observable domain value is invalid {Value}.",
                            new[] {nameof(Value)});
                    break;
                case ObservableType.URL:
                    r = new Regex(REGEX_URL);
                    if (!r.IsMatch(Value))
                        yield return new ValidationResult(
                            $"Observable url value is invalid {Value}.",
                            new[] {nameof(Value)});
                    break;
                case ObservableType.File:
                case ObservableType.Artefact:
                    foreach (var h in Hashes)
                    {
                        switch (h.HashType)
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
                            if (!r.IsMatch(h.Value))
                                yield return new ValidationResult(
                                    $"Observable hash value is invalid {h.Value}.",
                                    new[] {nameof(h.Value)});
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}