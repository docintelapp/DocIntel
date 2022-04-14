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

using System.ComponentModel;

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace DocIntel.Core.Authorization.Operations
{
    public static class SourceOperations
    {
        public static OperationAuthorizationRequirement Create =
            new() {Name = SourceOperationsConstants.CreateOperationName};

        public static OperationAuthorizationRequirement View =
            new() {Name = SourceOperationsConstants.ViewOperationName};

        public static OperationAuthorizationRequirement Edit =
            new() {Name = SourceOperationsConstants.EditOperationName};

        public static OperationAuthorizationRequirement Delete =
            new() {Name = SourceOperationsConstants.DeleteOperationName};

        public static OperationAuthorizationRequirement List =
            new() {Name = SourceOperationsConstants.ListOperationName};

        public static OperationAuthorizationRequirement Merge =
            new() {Name = SourceOperationsConstants.MergeOperationName};

        public static OperationAuthorizationRequirement Subscribe =
            new() {Name = SourceOperationsConstants.SubscribeOperationName};
    }

    [DisplayName("Source")]
    public class SourceOperationsConstants : IOperationConstants
    {
        [DisplayName("Create sources")] public static string CreateOperationName => "CreateSource";
        [DisplayName("View sources")] public static string ViewOperationName => "ViewSource";
        [DisplayName("Edit sources")] public static string EditOperationName => "EditSource";
        [DisplayName("Delete sources")] public static string DeleteOperationName => "DeleteSource";
        [DisplayName("List sources")] public static string ListOperationName => "ListSource";
        [DisplayName("Merge sources")] public static string MergeOperationName => "MergeSources";
        [DisplayName("Subscribe to sources")] public static string SubscribeOperationName => "SubscribeSource";
    }
}