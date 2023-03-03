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

using System.Security.Claims;
using System.Threading.Tasks;
using DocIntel.Core.Authorization.Operations;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Authorization;

namespace DocIntel.Core.Authorization
{
    public class AppAuthorizationService : IAppAuthorizationService
    {
        private readonly IAuthorizationService _authorizationService;

        public AppAuthorizationService(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        public async Task<bool> CanAddGroup(ClaimsPrincipal claimsPrincipal, Group role)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, GroupOperations.Create);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanUpdateGroup(ClaimsPrincipal claimsPrincipal, Group role)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, GroupOperations.Update);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewGroup(ClaimsPrincipal claimsPrincipal, Group role)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claimsPrincipal, role, GroupOperations.View);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteGroup(ClaimsPrincipal claimsPrincipal, Group role)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, GroupOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanAddGroupMember(ClaimsPrincipal claimsPrincipal, Group role, AppUser user)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, GroupOperations.AddGroupMember);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanRemoveGroupMember(ClaimsPrincipal claimsPrincipal, Group role, AppUser user)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, GroupOperations.RemoveGroupMember);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanCreateRole(ClaimsPrincipal claimsPrincipal, AppRole role)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claimsPrincipal, role, RoleOperations.Create);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewRole(ClaimsPrincipal claimsPrincipal, AppRole role)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, RoleOperations.Details);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditRole(ClaimsPrincipal claimsPrincipal, AppRole role)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claimsPrincipal, role, RoleOperations.Update);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteRole(ClaimsPrincipal claimsPrincipal, AppRole role)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claimsPrincipal, role, RoleOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewUserRole(ClaimsPrincipal claimsPrincipal, AppUser candidate, AppRole role)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, RoleOperations.ViewRole);
            var isAuthorized2 =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, candidate, RoleOperations.ViewRole);
            return isAuthorized.Succeeded & isAuthorized2.Succeeded;
        }

        public async Task<bool> CanAddUserRole(ClaimsPrincipal claimsPrincipal, AppUser candidate, AppRole role)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, RoleOperations.AddRole);
            var isAuthorized2 =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, candidate, RoleOperations.AddRole);
            return isAuthorized.Succeeded & isAuthorized2.Succeeded;
        }

        public async Task<bool> CanRemoveUserRole(ClaimsPrincipal claimsPrincipal, AppUser candidate, AppRole role)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, RoleOperations.RemoveRole);
            var isAuthorized2 =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, candidate, RoleOperations.RemoveRole);
            return isAuthorized.Succeeded & isAuthorized2.Succeeded;
        }

        public async Task<bool> CanEditRolePermissions(ClaimsPrincipal claimsPrincipal, AppRole role)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, role, RoleOperations.EditPermissions);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanCreateDocument(ClaimsPrincipal claimsPrincipal, Document document)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.Create);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanReadDocument(ClaimsPrincipal claimsPrincipal, Document document)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.Read);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditDocument(ClaimsPrincipal claimsPrincipal, Document document)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.Edit);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteDocument(ClaimsPrincipal claimsPrincipal, Document document)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDiscardDocument(ClaimsPrincipal claimsPrincipal, Document document)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.Discard);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDownloadDocument(ClaimsPrincipal claimsPrincipal, Document document)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.Download);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanSubscribeDocument(ClaimsPrincipal claimsPrincipal, Document document)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.Subscribe);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanAddTagToDocument(ClaimsPrincipal claimsPrincipal, Document document, Tag tag)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.AddTag);
            var isAuthorized2 =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, tag, DocumentOperations.AddTag);
            return isAuthorized.Succeeded & isAuthorized2.Succeeded;
        }

        public async Task<bool> CanRemoveTagFromDocument(ClaimsPrincipal claimsPrincipal, Document document, Tag tag)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.RemoveTag);
            var isAuthorized2 =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, tag, DocumentOperations.RemoveTag);
            return isAuthorized.Succeeded & isAuthorized2.Succeeded;
        }

        public async Task<bool> CanAddComment(ClaimsPrincipal claimsPrincipal, Document document, Comment comment)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.AddComment);
            var isAuthorized2 =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, comment, DocumentOperations.AddComment);
            return isAuthorized.Succeeded & isAuthorized2.Succeeded;
        }

        public async Task<bool> CanDeleteComment(ClaimsPrincipal claimsPrincipal, Comment comment)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, comment, DocumentOperations.DeleteComment);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditComment(ClaimsPrincipal claimsPrincipal, Comment comment)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, comment, DocumentOperations.EditComment);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewComment(ClaimsPrincipal claimsPrincipal, Comment comment)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, comment, DocumentOperations.ViewComment);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanCreateSource(ClaimsPrincipal claimsPrincipal, Source source)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, source, SourceOperations.Create);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewSource(ClaimsPrincipal claimsPrincipal, Source source)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, source, SourceOperations.View);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditSource(ClaimsPrincipal claimsPrincipal, Source source)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, source, SourceOperations.Edit);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteSource(ClaimsPrincipal claimsPrincipal, Source source)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, source, SourceOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanMergeSource(ClaimsPrincipal claimsPrincipal, Source[] sources)
        {
            if (sources != null)
            {
                foreach (var source in sources)
                {
                    var isAuthorized =
                        await _authorizationService.AuthorizeAsync(claimsPrincipal, source, SourceOperations.Merge);
                    if (!isAuthorized.Succeeded)
                        return false;
                }

                return true;
            }

            return (await _authorizationService.AuthorizeAsync(claimsPrincipal, default, SourceOperations.Merge))
                .Succeeded;
        }

        public async Task<bool> CanSubscribeToSource(ClaimsPrincipal claims, AppUser user, Source source)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claims, user, SourceOperations.Subscribe);
            var isAuthorized2 = await _authorizationService.AuthorizeAsync(claims, source, SourceOperations.Subscribe);
            return isAuthorized.Succeeded & isAuthorized2.Succeeded;
        }

        public async Task<bool> CanCreateTag(ClaimsPrincipal claimsPrincipal, Tag tag)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claimsPrincipal, tag, TagOperations.Create);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewTag(ClaimsPrincipal claimsPrincipal, Tag tag)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claimsPrincipal, tag, TagOperations.View);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditTag(ClaimsPrincipal claimsPrincipal, Tag tag)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claimsPrincipal, tag, TagOperations.Edit);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanMergeTags(ClaimsPrincipal claimsPrincipal, Tag[] tags)
        {
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    var isAuthorized =
                        await _authorizationService.AuthorizeAsync(claimsPrincipal, tag, TagOperations.Merge);
                    if (!isAuthorized.Succeeded)
                        return false;
                }

                return true;
            }

            return (await _authorizationService.AuthorizeAsync(claimsPrincipal, default, TagOperations.Merge)).Succeeded;
        }

        public async Task<bool> CanDeleteTag(ClaimsPrincipal claimsPrincipal, Tag tag)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claimsPrincipal, tag, TagOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanSubscribeToTag(ClaimsPrincipal claimsPrincipal, Tag tag)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, tag, TagOperations.Subscribe);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, facet, TagOperations.ViewFacet);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanCreateFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, facet, TagOperations.CreateFacet);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, facet, TagOperations.EditFacet);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, facet, TagOperations.DeleteFacet);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanSubscribeToFacetTag(ClaimsPrincipal claimsPrincipal, TagFacet facet)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, facet, TagOperations.SubscribeFacet);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanMergeFacets(ClaimsPrincipal claimsPrincipal, TagFacet[] facets)
        {
            if (facets != null)
            {
                foreach (var facet in facets)
                {
                    var isAuthorized =
                        await _authorizationService.AuthorizeAsync(claimsPrincipal, facet, TagOperations.MergeFacet);
                    if (!isAuthorized.Succeeded)
                        return false;
                }

                return true;
            }

            return (await _authorizationService.AuthorizeAsync(claimsPrincipal, default, TagOperations.Merge)).Succeeded;
        }

        public async Task<bool> CanViewUser(ClaimsPrincipal claim, AppUser user)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claim, user, UserOperations.View);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditUser(ClaimsPrincipal claim, AppUser user)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claim, user, UserOperations.Edit);
            if (claim.HasClaim("UserId", user.Id) &
                (await _authorizationService.AuthorizeAsync(claim, user, UserOperations.EditOwn)).Succeeded)
                return true;
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanCreateIncomingFeed(ClaimsPrincipal claimsPrincipal, Importer feed)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, feed, IncomingFeedOperations.Create);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewIncomingFeed(ClaimsPrincipal claimsPrincipal, Importer feed)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, feed, IncomingFeedOperations.View);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditIncomingFeed(ClaimsPrincipal claimsPrincipal, Importer feed)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, feed, IncomingFeedOperations.Edit);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteIncomingFeed(ClaimsPrincipal claimsPrincipal, Importer feed)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, feed, IncomingFeedOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanCreateScraper(ClaimsPrincipal claimsPrincipal, Scraper feed)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, feed, ScraperOperations.Create);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewScraper(ClaimsPrincipal claimsPrincipal, Scraper feed)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, feed, ScraperOperations.View);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditScraper(ClaimsPrincipal claimsPrincipal, Scraper feed)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, feed, ScraperOperations.Edit);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteScraper(ClaimsPrincipal claimsPrincipal, Scraper feed)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, feed, ScraperOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewImportRule(ClaimsPrincipal claimsPrincipal, ImportRuleSet rule)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, rule, ImportRuleOperations.View);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanCreateImportRule(ClaimsPrincipal claimsPrincipal, ImportRuleSet rule)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, rule, ImportRuleOperations.Create);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditImportRule(ClaimsPrincipal claimsPrincipal, ImportRuleSet rule)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, rule, ImportRuleOperations.Edit);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteImportRule(ClaimsPrincipal claimsPrincipal, ImportRuleSet rule)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, rule, ImportRuleOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanResetPassword(ClaimsPrincipal claims, AppUser user)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claims, user, UserOperations.ResetPassword);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanChangePassword(ClaimsPrincipal claims, AppUser user)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claims, user, UserOperations.ChangePassword);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanCreateUser(ClaimsPrincipal claims, AppUser user)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claims, user, UserOperations.Create);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanRemoveUser(ClaimsPrincipal claims, AppUser user)
        {
            var isAuthorized = await _authorizationService.AuthorizeAsync(claims, user, UserOperations.Remove);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanAddClassification(ClaimsPrincipal claimsPrincipal, Classification group)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, group, ClassificationOperations.Add);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanUpdateClassification(ClaimsPrincipal claimsPrincipal, Classification group)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, group, ClassificationOperations.Update);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteClassification(ClaimsPrincipal claimsPrincipal, Classification group)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, group, ClassificationOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewClassification(ClaimsPrincipal claimsPrincipal, Classification group)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, group, ClassificationOperations.View);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanAddDocumentFile(ClaimsPrincipal claimsPrincipal, Document document) {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.AddFile);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewDocumentFile(ClaimsPrincipal claimsPrincipal, DocumentFile document) {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.ViewFile);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanEditDocumentFile(ClaimsPrincipal claimsPrincipal, DocumentFile document) {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.EditFile);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteDocumentFile(ClaimsPrincipal claimsPrincipal, DocumentFile document) {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, document, DocumentOperations.DeleteFile);
            return isAuthorized.Succeeded;
        }


        public async Task<bool> CanAddSavedSearch(ClaimsPrincipal claimsPrincipal, SavedSearch group)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, group, SavedSearchOperations.Add);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanUpdateSavedSearch(ClaimsPrincipal claimsPrincipal, SavedSearch group)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, group, SavedSearchOperations.Update);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanDeleteSavedSearch(ClaimsPrincipal claimsPrincipal, SavedSearch group)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, group, SavedSearchOperations.Delete);
            return isAuthorized.Succeeded;
        }

        public async Task<bool> CanViewSavedSearch(ClaimsPrincipal claimsPrincipal, SavedSearch group)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, group, SavedSearchOperations.View);
            return isAuthorized.Succeeded;
        }


        public async Task<bool> CanListImportRules(ClaimsPrincipal claimsPrincipal, ImportRuleSet rule)
        {
            var isAuthorized =
                await _authorizationService.AuthorizeAsync(claimsPrincipal, rule, ImportRuleOperations.List);
            return isAuthorized.Succeeded;
        }
    }
}