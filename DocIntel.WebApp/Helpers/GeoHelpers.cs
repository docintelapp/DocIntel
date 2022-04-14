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
using System.Globalization;
using System.IO;
using System.Linq;

using CsvHelper;

namespace DocIntel.WebApp.Helpers
{
    public static class GeoHelpers
    {
        private static IEnumerable<UNSDCountryRecord> _cachedList;
        
        public static IEnumerable<UNSDCountryRecord> GetUNList()
        {
            /*
            if (_cachedList != null)
                return _cachedList;
            */
            
            var dir = Directory.GetCurrentDirectory();
            Console.WriteLine(dir);
            var file = Path.Combine(dir, "unsd-m49.csv");
            if (File.Exists(file))
            {
                Console.WriteLine("FILE EXISTS");
                TextReader reader = new StreamReader(file);
                var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                return _cachedList = csvReader.GetRecords<UNSDCountryRecord>();
            }
            else
            {
                Console.WriteLine("FILE DOES NOT EXIST");
                return _cachedList = Enumerable.Empty<UNSDCountryRecord>();
            }
        }

        public static IEnumerable<string> GetISO2Codes(string m49)
        {
            var list = GetUNList().ToList();
            
            // Get the global
            var global = list.Where(_ => _.GlobalCode == m49).Select(_ => _.ISO2Code);
            
            // Get the region
            var regions = list.Where(_ => _.RegionCode == m49).Select(_ => _.ISO2Code);
            
            // Get the sub-region
            var subRegions = list.Where(_ => _.SubregionCode == m49).Select(_ => _.ISO2Code);
            
            // Get the intermediate regions
            var intermediateRegions = list.Where(_ => _.IntermediateRegionCode == m49).Select(_ => _.ISO2Code);
            
            // Get the countries or areas
            var countriesOrAreas = list.Where(_ => _.M49Code == m49).Select(_ => _.ISO2Code);

            return global.Union(regions).Union(subRegions).Union(intermediateRegions).Union(countriesOrAreas)
                .Where(_ => !string.IsNullOrEmpty(_)).Distinct();
        }
    }
}