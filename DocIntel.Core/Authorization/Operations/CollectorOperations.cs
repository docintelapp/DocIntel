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

using System.ComponentModel;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace DocIntel.Core.Authorization.Operations
{
    public static class CollectorOperations
    {
        public static OperationAuthorizationRequirement Create =
            new() {Name = CollectorOperationsConstants.CreateCollectorOperationName};

        public static OperationAuthorizationRequirement List =
            new() {Name = CollectorOperationsConstants.ListCollectorOperationName};

        public static OperationAuthorizationRequirement View =
            new() {Name = CollectorOperationsConstants.ViewCollectorOperationName};

        public static OperationAuthorizationRequirement Edit =
            new() {Name = CollectorOperationsConstants.EditCollectorOperationName};

        public static OperationAuthorizationRequirement Delete =
            new() {Name = CollectorOperationsConstants.DeleteCollectorOperationName};
    }

    [DisplayName("Collectors")]
    public class CollectorOperationsConstants : IOperationConstants
    {
        [DisplayName("Create collectors")] public static string CreateCollectorOperationName => "CreateCollector";
        [DisplayName("List collectors")] public static string ListCollectorOperationName => "ListCollectors";
        [DisplayName("View collectors")] public static string ViewCollectorOperationName => "ViewCollector";
        [DisplayName("Edit collectors")] public static string EditCollectorOperationName => "EditCollector";
        [DisplayName("Delete collectors")] public static string DeleteCollectorOperationName => "DeleteCollector";
    }
}