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
    public static class UserOperations
    {
        public static OperationAuthorizationRequirement View =
            new() {Name = UserOperationsConstants.ViewProfileOperationName};

        public static OperationAuthorizationRequirement Edit =
            new() {Name = UserOperationsConstants.EditUsersOperationName};

        public static OperationAuthorizationRequirement EditOwn =
            new() {Name = UserOperationsConstants.EditOwnUsersOperationName};

        public static OperationAuthorizationRequirement Create =
            new() {Name = UserOperationsConstants.CreateOperationName};

        public static OperationAuthorizationRequirement ChangePassword =
            new() {Name = UserOperationsConstants.ChangePasswordOperationName};

        public static OperationAuthorizationRequirement ResetPassword =
            new() {Name = UserOperationsConstants.ResetPasswordOperationName};

        public static OperationAuthorizationRequirement Remove =
            new() {Name = UserOperationsConstants.RemoveOperationName};
    }

    [DisplayName("Users")]
    public class UserOperationsConstants : IOperationConstants
    {
        [DisplayName("View user profiles")] public static string ViewProfileOperationName => "ViewProfile";
        [DisplayName("Edit users")] public static string EditUsersOperationName => "EditUser";
        [DisplayName("Edit own user")] public static string EditOwnUsersOperationName => "EditOwnUser";
        [DisplayName("Change passwords")] public static string ChangePasswordOperationName => "ChangePassword";
        [DisplayName("Reset passwords")] public static string ResetPasswordOperationName => "ResetPassword";
        [DisplayName("Create users")] public static string CreateOperationName => "CreateUser";
        [DisplayName("Delete users")] public static string RemoveOperationName => "RemoveUser";
    }
}