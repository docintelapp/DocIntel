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
    public static class DocumentOperations
    {
        public static readonly OperationAuthorizationRequirement Search =
            new() {Name = DocumentOperationsConstants.SearchOperationName};

        public static readonly OperationAuthorizationRequirement Create =
            new() {Name = DocumentOperationsConstants.RegisterOperationName};

        public static readonly OperationAuthorizationRequirement Read =
            new() {Name = DocumentOperationsConstants.ViewOperationName};

        public static readonly OperationAuthorizationRequirement Edit =
            new() {Name = DocumentOperationsConstants.EditOperationName};
        
        public static readonly OperationAuthorizationRequirement Delete =
            new() {Name = DocumentOperationsConstants.DeleteOperationName};
        
        public static readonly OperationAuthorizationRequirement Discard =
            new() {Name = DocumentOperationsConstants.DiscardOperationName};

        public static readonly OperationAuthorizationRequirement Download =
            new() {Name = DocumentOperationsConstants.DownloadOperationName};

        public static readonly OperationAuthorizationRequirement Subscribe =
            new() {Name = DocumentOperationsConstants.SubscribeOperationName};

        public static readonly OperationAuthorizationRequirement AddTag =
            new() {Name = DocumentOperationsConstants.AddTagOperationName};

        public static readonly OperationAuthorizationRequirement RemoveTag =
            new() {Name = DocumentOperationsConstants.RemoveTagOperationName};

        public static readonly OperationAuthorizationRequirement AddComment =
            new() {Name = DocumentOperationsConstants.AddCommentOperationName};

        public static readonly OperationAuthorizationRequirement ViewComment =
            new() {Name = DocumentOperationsConstants.ViewCommentOperationName};

        public static readonly OperationAuthorizationRequirement DeleteComment =
            new() {Name = DocumentOperationsConstants.DeleteCommentOperationName};

        public static readonly OperationAuthorizationRequirement EditComment =
            new() {Name = DocumentOperationsConstants.EditCommentOperationName};

        public static readonly OperationAuthorizationRequirement DeleteOwnComment =
            new() {Name = DocumentOperationsConstants.DeleteOwnCommentOperationName};

        public static readonly OperationAuthorizationRequirement EditOwnComment =
            new() {Name = DocumentOperationsConstants.EditOwnCommentOperationName};

        public static readonly OperationAuthorizationRequirement AddFile =
            new() {Name = DocumentOperationsConstants.AddFileOperationName};
        
        public static readonly OperationAuthorizationRequirement ViewFile =
            new() {Name = DocumentOperationsConstants.ViewFileOperationName};
        
        public static readonly OperationAuthorizationRequirement DeleteFile =
            new() {Name = DocumentOperationsConstants.DeleteFileOperationName};
        
        public static readonly OperationAuthorizationRequirement EditFile =
            new() {Name = DocumentOperationsConstants.EditFileOperationName};

    }

    [DisplayName("Document")]
    public class DocumentOperationsConstants : IOperationConstants
    {
        [DisplayName("View documents")] public static string ViewOperationName => "ViewDocument";
        [DisplayName("Search documents")] public static string SearchOperationName => "SearchDocument";
        [DisplayName("Register documents")] public static string RegisterOperationName => "RegisterDocument";
        [DisplayName("Edit documents")] public static string EditOperationName => "EditDocument";
        [DisplayName("Delete documents")] public static string DeleteOperationName => "DeleteDocument";
        [DisplayName("Discard documents")] public static string DiscardOperationName => "DiscardDocument";
        [DisplayName("Download documents")] public static string DownloadOperationName => "DownloadDocument";
        [DisplayName("Subscribe to documents")] public static string SubscribeOperationName => "SubscribeToDoc";
        [DisplayName("Add tags to documents")] public static string AddTagOperationName => "AddTagsToDoc";
        [DisplayName("Remove tags from documents")] public static string RemoveTagOperationName => "RemoveTagsToDoc";
        [DisplayName("Comment documents")] public static string AddCommentOperationName => "AddComment";
        [DisplayName("View comments")] public static string ViewCommentOperationName => "ViewComment";
        [DisplayName("Delete comments")] public static string DeleteCommentOperationName => "DeleteComment";
        [DisplayName("Delete own comments")] public static string DeleteOwnCommentOperationName => "DeleteOwnComment";
        [DisplayName("Edit comments")] public static string EditCommentOperationName => "EditComment";
        [DisplayName("Edit own comments")] public static string EditOwnCommentOperationName => "EditOwnComment";
        [DisplayName("Add files to documents")] public static string AddFileOperationName => "AddFile";
        [DisplayName("View files")] public static string ViewFileOperationName => "ViewFile";
        [DisplayName("Delete files")] public static string DeleteFileOperationName => "DeleteFile";
        [DisplayName("Edit files")] public static string EditFileOperationName => "EditFile";
    }
}