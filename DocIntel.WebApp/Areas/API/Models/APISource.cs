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

using DocIntel.Core.Models;

namespace DocIntel.WebApp.Areas.API.Models
{
    public class APISource
    {
        public Guid SourceId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string HomePage { get; set; }
        public string RSSFeed { get; set; }
        public string Facebook { get; set; }
        public string Twitter { get; set; }
        public string Reddit { get; set; }
        public string LinkedIn { get; set; }

        public IEnumerable<string> Keywords { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public APIAppUser RegisteredBy { get; set; }
        public APIAppUser LastModifiedBy { get; set; }

        public SourceReliability Reliability { get; set; }
        public int BiasedWording { get; set; } = -1;
        public int Factual { get; set; } = -1;
        public int StoryChoice { get; set; } = -1;
        public int PoliticalAffiliation { get; set; } = -1;

        public int FactualScore
        {
            get
            {
                if ((BiasedWording < 0) | (Factual < 0) | (StoryChoice < 0) | (PoliticalAffiliation < 0))
                    return -1;

                return (int) Math.Round((BiasedWording + Factual + StoryChoice + PoliticalAffiliation) / 4.0d);
            }
        }

        public PoliticalSpectrum PoliticalSpectrum { get; set; }

        public string Country { get; set; }
    }
}