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
    public static class IncomingFeedOperations
    {
        public static OperationAuthorizationRequirement Create =
            new() {Name = IncomingFeedOperationsConstants.CreateIncomingFeedOperationName};

        public static OperationAuthorizationRequirement View =
            new() {Name = IncomingFeedOperationsConstants.ViewIncomingFeedOperationName};

        public static OperationAuthorizationRequirement Edit =
            new() {Name = IncomingFeedOperationsConstants.EditIncomingFeedOperationName};

        public static OperationAuthorizationRequirement Delete =
            new() {Name = IncomingFeedOperationsConstants.DeleteIncomingFeedOperationName};
    }

    [DisplayName("Incoming feeds")]
    public class IncomingFeedOperationsConstants : IOperationConstants
    {
        [DisplayName("Create importers")] public static string CreateIncomingFeedOperationName => "CreateIncomingFeed";
        [DisplayName("View importers")] public static string ViewIncomingFeedOperationName => "ViewIncomingFeed";
        [DisplayName("Edit importers")] public static string EditIncomingFeedOperationName => "EditIncomingFeed";
        [DisplayName("Delete importers")] public static string DeleteIncomingFeedOperationName => "DeleteIncomingFeed";
    }
}