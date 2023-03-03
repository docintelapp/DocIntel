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
using System.Threading.Tasks;

using DocIntel.Core.Models;
using Json.Schema;

namespace DocIntel.Core.Scrapers
{
    public interface IScraper
    {
        bool HasSettings { get; }
        IEnumerable<string> Patterns { get; }
        ScraperInformation Get();
        Scraper Install();

        /// <summary>
        ///     Scrape the content indicated by the submitted message.
        /// </summary>
        /// <param name="message">The content to scrape.</param>
        /// <returns><c>True</c> if the next scrapers should process the message, <c>False</c> otherwise.</returns>
        Task<bool> Scrape(SubmittedDocument message);
        
        JsonSchema GetSettingsSchema();
        Type GetSettingsType();
        string GetSettingsView();
    }
}