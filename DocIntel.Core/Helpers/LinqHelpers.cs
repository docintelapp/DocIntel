/*
 * DocIntel
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
using System.Linq;

using DocIntel.Core.Models;

namespace DocIntel.Core.Helpers
{
    public static class LinqHelpers
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }

        public static string ObservableValue(this Observable o)
        {
            if (new[] {ObservableType.Artefact, ObservableType.File}.Contains(o.Type))
            {
                var h = o.Hashes.FirstOrDefault();
                return h?.Value;
            }

            return o.Value;
        }

        public static string FirstCharToUpper(this string input)
        {
            return input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => input.First().ToString().ToUpper() + input.Substring(1).ToLower()
            };
        }
        
        public static IEnumerable<T> Except<T, TKey>(this IEnumerable<T> items, IEnumerable<T> other,
            Func<T, TKey> getKeyFunc)
        {
            return items
                .GroupJoin(other, getKeyFunc, getKeyFunc, (item, tempItems) => new {item, tempItems})
                .SelectMany(t => t.tempItems.DefaultIfEmpty(), (t, temp) => new {t, temp})
                .Where(t => ReferenceEquals(null, t.temp) || t.temp.Equals(default(T)))
                .Select(t => t.t.item);
        }

        // TODO Really ugly. Should be move to a separate STIX namespace too.
        public static string getStixObjectNameFromFacet(this TagFacet tf)
        {
            switch (tf.Title.ToLower())
            {
                case "malwarefamilies":
                case "malware":
                    return "malware";
                case "person":
                case "company":
                    return "identity";
                case "vulnerability":
                    return "vulnerability";
                case "software":
                case "tool":
                    return "software";
                case "attack pattern":
                    return "technique";
                case "group":
                case "actor":
                    return "actor";
                case "campaign":
                    return "campaign";
                case "affectedIndustry":
                    return "industry";
                default:
                    return "";
            }
        }

        // TODO Really ugly. Should be move to a separate ThreatQuotient namespace too.
        public static string getThreatQObjectNameFromFacet(this TagFacet tf)
        {
            switch (tf.Title.ToLower())
            {
                case "malwarefamilies":
                case "malware":
                    return "Malware";
                case "person":
                case "company":
                    return "Identity";
                case "vulnerability":
                    return "Vulnerability";
                case "software":
                case "tool":
                    return "Tool";
                case "attack pattern":
                    return "Attack Pattern";
                case "group":
                case "actor":
                    return "Adversary";
                case "campaign":
                    return "Campaign";
                default:
                    return "";
            }
        }
    }
}