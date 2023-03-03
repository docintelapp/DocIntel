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
    public static class GroupOperations
    {
        public static readonly OperationAuthorizationRequirement Create =
            new() {Name = GroupOperationsConstants.CreateOperationName};

        public static readonly OperationAuthorizationRequirement Update =
            new() {Name = GroupOperationsConstants.UpdateOperationName};

        public static readonly OperationAuthorizationRequirement View =
            new() {Name = GroupOperationsConstants.ViewOperationName};

        public static readonly OperationAuthorizationRequirement Delete =
            new() {Name = GroupOperationsConstants.DeleteOperationName};

        public static readonly OperationAuthorizationRequirement AddGroupMember =
            new() {Name = GroupOperationsConstants.AddGroupMemberName};

        public static readonly OperationAuthorizationRequirement RemoveGroupMember =
            new() {Name = GroupOperationsConstants.RemoveGroupMemberName};
    }

    [DisplayName("Group")]
    public class GroupOperationsConstants : IOperationConstants
    {
        [DisplayName("Create a new group")] public static string CreateOperationName => "CreateGroup";
        [DisplayName("Update a group")] public static string UpdateOperationName => "UpdateGroup";
        [DisplayName("Delete a group")] public static string DeleteOperationName => "DeleteGroup";
        [DisplayName("Add a user to a group")] public static string AddGroupMemberName => "AddGroupMember";
        [DisplayName("Remove a user from a group")] public static string RemoveGroupMemberName => "RemoveGroupMember";
        [DisplayName("View all groups")] public static string ViewOperationName => "ViewGroup";
    }
}