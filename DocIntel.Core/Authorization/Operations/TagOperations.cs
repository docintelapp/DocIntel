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
    public static class TagOperations
    {
        public static OperationAuthorizationRequirement Create =
            new() {Name = TagOperationsConstants.CreateOperationName};

        public static OperationAuthorizationRequirement View =
            new() {Name = TagOperationsConstants.ViewOperationName};

        public static OperationAuthorizationRequirement Edit =
            new() {Name = TagOperationsConstants.EditOperationName};

        public static OperationAuthorizationRequirement Merge =
            new() {Name = TagOperationsConstants.MergeOperationName};

        public static OperationAuthorizationRequirement Delete =
            new() {Name = TagOperationsConstants.DeleteOperationName};

        public static OperationAuthorizationRequirement List =
            new() {Name = TagOperationsConstants.ListOperationName};

        public static OperationAuthorizationRequirement Subscribe =
            new() {Name = TagOperationsConstants.SubscribeOperationName};

        public static OperationAuthorizationRequirement ViewFacet =
            new() {Name = TagOperationsConstants.ViewFacetOperationName};

        public static OperationAuthorizationRequirement CreateFacet =
            new() {Name = TagOperationsConstants.CreateFacetOperationName};

        public static OperationAuthorizationRequirement EditFacet =
            new() {Name = TagOperationsConstants.EditFacetOperationName};

        public static OperationAuthorizationRequirement DeleteFacet =
            new() {Name = TagOperationsConstants.DeleteFacetOperationName};

        public static OperationAuthorizationRequirement SubscribeFacet =
            new() {Name = TagOperationsConstants.SubscribeFacetOperationName};

        public static OperationAuthorizationRequirement MergeFacet =
            new() {Name = TagOperationsConstants.MergeFacetOperationName};
    }

    [DisplayName("Tag and facets")]
    public class TagOperationsConstants : IOperationConstants
    {
        [DisplayName("Create tags")] public static string CreateOperationName => "CreateTag";
        [DisplayName("View tags")] public static string ViewOperationName => "ViewTag";
        [DisplayName("Edit tags")] public static string EditOperationName => "EditTag";
        [DisplayName("Merge tags")] public static string MergeOperationName => "MergeTag";
        [DisplayName("Delete tags")] public static string DeleteOperationName => "DeleteTag";
        [DisplayName("List tags")] public static string ListOperationName => "ListTag";
        [DisplayName("Subscribe to tags")] public static string SubscribeOperationName => "SubscribeTag";
        [DisplayName("View facets")] public static string ViewFacetOperationName => "ViewFacet";
        [DisplayName("Create facets")] public static string CreateFacetOperationName => "CreateFacet";
        [DisplayName("Edit facets")] public static string EditFacetOperationName => "EditFacet";
        [DisplayName("Delete facets")] public static string DeleteFacetOperationName => "DeleteFacet";
        [DisplayName("Subscribe facets")] public static string SubscribeFacetOperationName => "SubscribeFacet";
        [DisplayName("Merge facets")] public static string MergeFacetOperationName => "MergeFacet";
    }
}