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

using Microsoft.AspNetCore.Mvc;

namespace DocIntel.WebApp.ViewModels.DocumentViewModel
{
    public class DocumentObservablesViewModel
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; }
        public ICollection<DocumentFile> Files { get; set; }
        public ObservableViewModel[] Observables { get; set; }
    }
    
    public class OTest
    {
        public bool IsAccepted { get; set; }
        public bool IsWhitelisted { get; set; }
        public ObservableStatus History { get; set; }
        public Guid Id { get; set; }
        public ObservableType Type { get; set; }
        public string Value { get; set; }
        public ObservableStatus Status { get; set; }
        public IList<ObservableHash> Hashes { get; set; }
        public IList<Tag> Tags { get; set; }
        public ObservableHashType HashType { get; set; }
    }
}