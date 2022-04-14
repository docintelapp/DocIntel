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

namespace DocIntel.Core.Models
{
    public class Observable : IEqualityComparer<Observable>
    {
        public Observable()
        {
            Id = Guid.NewGuid();
            RegistrationDate = DateTime.UtcNow;
            ModificationDate = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public ObservableType Type { get; set; }
        public string Value { get; set; }
        public ObservableStatus History { get; set; }
        public ObservableStatus Status { get; set; }

        public DateTime RegistrationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public string RegisteredById { get; set; }
        public string LastModifiedById { get; set; }
        public IList<ObservableHash> Hashes { get; set; }

        public bool Equals(Observable x, Observable y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;
            if (x.Type != y.Type) return false;
            if (x.Type == ObservableType.Artefact || x.Type == ObservableType.File)
                return x.Hashes[0].Value == y.Hashes[0].Value;
            return x.Value == y.Value;
        }

        public int GetHashCode(Observable obj)
        {
            if (obj.Type == ObservableType.Artefact || obj.Type == ObservableType.File)
                return obj.Type.GetHashCode() ^ obj.Hashes[0].Value.GetHashCode();
            return obj.Type.GetHashCode() ^ obj.Value.GetHashCode();
        }
    }
}