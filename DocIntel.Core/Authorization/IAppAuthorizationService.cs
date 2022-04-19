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

using System.Security.Claims;
using System.Threading.Tasks;

using DocIntel.Core.Models;

namespace DocIntel.Core.Authorization
{
    // TODO Check why some methods appears to be unused, that is HIGHLY suspicious
    public interface IAppAuthorizationService
    {
        Task<bool> CanCreateDocument(ClaimsPrincipal claimsPrincipal, Document document);
        Task<bool> CanReadDocument(ClaimsPrincipal claimsPrincipal, Document document);
        Task<bool> CanEditDocument(ClaimsPrincipal claimsPrincipal, Document document);
        Task<bool> CanDeleteDocument(ClaimsPrincipal claimsPrincipal, Document document);
        Task<bool> CanDownloadDocument(ClaimsPrincipal claimsPrincipal, Document document);
        Task<bool> CanSubscribeDocument(ClaimsPrincipal claimsPrincipal, Document document);
        Task<bool> CanAddTagToDocument(ClaimsPrincipal claimsPrincipal, Document document, Tag tag);
        Task<bool> CanRemoveTagFromDocument(ClaimsPrincipal claimsPrincipal, Document document, Tag tag);
        Task<bool> CanDiscardDocument(ClaimsPrincipal contextClaims, Document document);
        
        Task<bool> CanAddDocumentFile(ClaimsPrincipal claimsPrincipal, Document document);
        Task<bool> CanViewDocumentFile(ClaimsPrincipal claimsPrincipal, DocumentFile document);
        Task<bool> CanEditDocumentFile(ClaimsPrincipal claimsPrincipal, DocumentFile document);
        Task<bool> CanDeleteDocumentFile(ClaimsPrincipal claimsPrincipal, DocumentFile document);

        Task<bool> CanAddComment(ClaimsPrincipal claimsPrincipal, Document document, Comment comment);
        Task<bool> CanDeleteComment(ClaimsPrincipal claimsPrincipal, Comment comment);
        Task<bool> CanEditComment(ClaimsPrincipal claimsPrincipal, Comment comment);
        Task<bool> CanViewComment(ClaimsPrincipal claimsPrincipal, Comment comment);

        Task<bool> CanViewRole(ClaimsPrincipal claimsPrincipal, AppRole role);
        Task<bool> CanCreateRole(ClaimsPrincipal claimsPrincipal, AppRole role);
        Task<bool> CanEditRole(ClaimsPrincipal claimsPrincipal, AppRole role);
        Task<bool> CanDeleteRole(ClaimsPrincipal claimsPrincipal, AppRole role);
        Task<bool> CanViewUserRole(ClaimsPrincipal claimsPrincipal, AppUser candidate, AppRole role);
        Task<bool> CanAddUserRole(ClaimsPrincipal claimsPrincipal, AppUser candidate, AppRole role);
        Task<bool> CanRemoveUserRole(ClaimsPrincipal claimsPrincipal, AppUser candidate, AppRole role);
        Task<bool> CanEditRolePermissions(ClaimsPrincipal claimsPrincipal, AppRole role);

        Task<bool> CanCreateSource(ClaimsPrincipal claimsPrincipal, Source source);
        Task<bool> CanViewSource(ClaimsPrincipal claimsPrincipal, Source source);
        Task<bool> CanEditSource(ClaimsPrincipal claimsPrincipal, Source source);
        Task<bool> CanDeleteSource(ClaimsPrincipal claimsPrincipal, Source source);
        Task<bool> CanMergeSource(ClaimsPrincipal claimsPrincipal, Source[] sources);
        Task<bool> CanSubscribeToSource(ClaimsPrincipal claim, AppUser user, Source source);

        Task<bool> CanCreateTag(ClaimsPrincipal claimsPrincipal, Tag tag);
        Task<bool> CanViewTag(ClaimsPrincipal claimsPrincipal, Tag tag);
        Task<bool> CanEditTag(ClaimsPrincipal claimsPrincipal, Tag tag);
        Task<bool> CanMergeTags(ClaimsPrincipal claimsPrincipal, Tag[] tags);
        Task<bool> CanDeleteTag(ClaimsPrincipal claimsPrincipal, Tag tag);
        Task<bool> CanSubscribeToTag(ClaimsPrincipal claimsPrincipal, Tag tag);

        Task<bool> CanViewFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet);
        Task<bool> CanCreateFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet);
        Task<bool> CanEditFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet);
        Task<bool> CanDeleteFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet);
        Task<bool> CanSubscribeToFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet);
        Task<bool> CanMergeFacets(ClaimsPrincipal claimsPrincipal, TagFacet[] facet);

        Task<bool> CanCreateUser(ClaimsPrincipal claimsPrincipal, AppUser user);
        Task<bool> CanViewUser(ClaimsPrincipal claimsPrincipal, AppUser user);
        Task<bool> CanEditUser(ClaimsPrincipal claimsPrincipal, AppUser user);

        Task<bool> CanCreateIncomingFeed(ClaimsPrincipal claimsPrincipal, Importer feed);
        Task<bool> CanViewIncomingFeed(ClaimsPrincipal claimsPrincipal, Importer feed);
        Task<bool> CanEditIncomingFeed(ClaimsPrincipal claimsPrincipal, Importer feed);
        Task<bool> CanDeleteIncomingFeed(ClaimsPrincipal claimsPrincipal, Importer feed);

        Task<bool> CanCreateScraper(ClaimsPrincipal claimsPrincipal, Scraper feed);
        Task<bool> CanViewScraper(ClaimsPrincipal claimsPrincipal, Scraper feed);
        Task<bool> CanEditScraper(ClaimsPrincipal claimsPrincipal, Scraper feed);
        Task<bool> CanDeleteScraper(ClaimsPrincipal claimsPrincipal, Scraper feed);

        Task<bool> CanViewImportRule(ClaimsPrincipal claimsPrincipal, ImportRuleSet importRule);
        Task<bool> CanCreateImportRule(ClaimsPrincipal claimsPrincipal, ImportRuleSet importRule);
        Task<bool> CanEditImportRule(ClaimsPrincipal claimsPrincipal, ImportRuleSet importRule);
        Task<bool> CanDeleteImportRule(ClaimsPrincipal claimsPrincipal, ImportRuleSet importRule);

        Task<bool> CanResetPassword(ClaimsPrincipal claims, AppUser user);
        Task<bool> CanChangePassword(ClaimsPrincipal claims, AppUser user);
        Task<bool> CanRemoveUser(ClaimsPrincipal claims, AppUser user);


        Task<bool> CanAddGroup(ClaimsPrincipal claims, Group group);
        Task<bool> CanUpdateGroup(ClaimsPrincipal claims, Group group);
        Task<bool> CanDeleteGroup(ClaimsPrincipal claims, Group group);
        Task<bool> CanViewGroup(ClaimsPrincipal claims, Group group);
        Task<bool> CanAddGroupMember(ClaimsPrincipal claims, Group group, AppUser member);
        Task<bool> CanRemoveGroupMember(ClaimsPrincipal claims, Group group, AppUser member);

        Task<bool> CanAddClassification(ClaimsPrincipal claims, Classification group);
        Task<bool> CanUpdateClassification(ClaimsPrincipal claims, Classification group);
        Task<bool> CanDeleteClassification(ClaimsPrincipal claims, Classification group);
        Task<bool> CanViewClassification(ClaimsPrincipal claims, Classification group);
    }
}