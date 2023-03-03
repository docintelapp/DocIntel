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

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Logging
{
    // TODO To be improved and generalized. The numbering of the eventID could be better implemented.
    public static class EventIDs
    {
        public static EventId LoginAttempt = new EventId(10000, "login-attempt");
        public static EventId LoginSuccessful = new EventId(10001, "login-success");
        public static EventId LoginFailed = new EventId(10002, "login-fail");
        public static EventId LoginError = new EventId(10003, "login-error");
        
        public static EventId RegistrationClosed = new EventId(10004, "registration-closed");
        public static EventId RegistrationSuccessful = new EventId(10005, "registration-success");
        public static EventId RegistrationFailed = new EventId(10006, "registration-fail");
        public static EventId RegistrationError = new EventId(10007, "registration-error");
        public static EventId LogoutSuccessful = new EventId(10008, "logout-success");
        public static EventId EditFailed = new EventId(10009, "user-profile-edit-fail");
        public static EventId EditSuccessful = new EventId(10010, "user-profile-edit-success");
        public static EventId NotificationNotFound = new EventId(10011, "notification-not-found");
        public static EventId NotificationStatusFailed = new EventId(10012, "notification-status-update-fail");
        public static EventId NotificationStatusSuccessful = new EventId(10013, "notification-status-update-success");
        public static EventId NotificationDeleteFailed = new EventId(10014, "notification-delete-fail");
        public static EventId NotificationDeleteSuccessful = new EventId(10015, "notification-delete-success");
        public static EventId ProfilePictureFailed = new EventId(10016, "profile-picture-fail");
        public static EventId ProfilePictureSuccessful = new EventId(10017, "profile-picture-success");
        public static EventId APILoginAttempt = APIEventId(LoginAttempt);
        public static EventId APILoginFailed = APIEventId(LoginFailed);
        public static EventId APILoginSuccessful = APIEventId(LoginSuccessful);
        public static EventId CreateCommentFailed = new EventId(11000, "create-comment-fail");
        public static EventId CreateCommentSuccessful = new EventId(11001, "create-comment-success");
        public static EventId UpdateCommentFailed = new EventId(11002, "edit-comment-fail");
        public static EventId UpdateCommentSuccessful = new EventId(11003, "edit-comment-success");
        public static EventId DeleteCommentFailed = new EventId(11004, "delete-comment-fail");
        public static EventId DeleteCommentSuccessful = new EventId(11005, "delete-comment-success");
        public static EventId APICreateCommentFailed = APIEventId(CreateCommentFailed);
        public static EventId APICreateCommentSuccessful = APIEventId(CreateCommentSuccessful);
        public static EventId APIUpdateCommentFailed = APIEventId(UpdateCommentFailed);
        public static EventId APIUpdateCommentSuccessful = APIEventId(UpdateCommentSuccessful);
        public static EventId APIDeleteCommentFailed = APIEventId(DeleteCommentFailed);
        public static EventId APIDeleteCommentSuccessful = APIEventId(DeleteCommentSuccessful);
        public static EventId APIListIncomingFeedSuccessful = new EventId(23000, "list-incoming-feed-success");
        public static EventId APIListIncomingFeedFailed = new EventId(23001, "list-incoming-feed-fail");
        public static EventId APIDetailsIncomingFeedSuccessful = new EventId(23002, "details-incoming-feed-success");
        public static EventId APIDetailsIncomingFeedFailed = new EventId(23003, "details-incoming-feed-fail");
        public static EventId APIEditIncomingFeedSuccessful = new EventId(23004, "edit-incoming-feed-success");
        public static EventId APIEditIncomingFeedFailed = new EventId(23005, "edit-incoming-feed-fail");
        public static EventId APIEditIncomingFeedError = new EventId(23005, "edit-incoming-feed-error");
        public static EventId DeleteIncomingFeedSuccess= new EventId(23006, "delete-incoming-feed-success");
        public static EventId DeleteIncomingFeedFailed = new EventId(23007, "delete-incoming-feed-fail");

        public static EventId APICreateSourceSuccessful = new EventId(25000, "create-source-success");
        public static EventId APICreateSourceFailed = new EventId(25001, "create-source-fail");
        public static EventId APIEditSourceSuccessful = new EventId(25002, "edit-source-success");
        public static EventId APIEditSourceFailed = new EventId(25003, "edit-source-fail");
        public static EventId APIDeleteSourceSuccessful = new EventId(25004, "delete-source-success");
        public static EventId APIDeleteSourceFailed = new EventId(25005, "delete-source-fail");
        public static EventId APIListSourceSuccessful = new EventId(25006, "list-source-success");
        public static EventId APIListSourceFailed = new EventId(25007, "list-source-fail");
        public static EventId APIDetailsSourceSuccessful = new EventId(25008, "details-source-success");
        public static EventId APIDetailsSourceFailed = new EventId(25009, "details-source-fail");
        public static EventId APIMergeSourceSuccessful = new EventId(25010, "merge-source-success");
        public static EventId APIMergeSourceFailed = new EventId(25011, "merge-source-fail");
        public static EventId APISubscribeSourceSuccessful = new EventId(25012, "subscribe-source-success");
        public static EventId APISubscribeSourceFailed = new EventId(25013, "subscribe-source-fail");
        public static EventId APISatisticsSourceSuccessful = new EventId(25014, "statistics-source-success");
        public static EventId APISatisticsSourceFailed = new EventId(25015, "statistics-source-fail");

        public static EventId APICreateTagSuccessful = new EventId(26000, "create-tag-success");
        public static EventId APICreateTagFailed = new EventId(26001, "create-tag-fail");
        public static EventId APIEditTagSuccessful = new EventId(26002, "edit-tag-success");
        public static EventId APIEditTagFailed = new EventId(26003, "edit-tag-fail");
        public static EventId APIDeleteTagSuccessful = new EventId(26004, "delete-tag-success");
        public static EventId APIDeleteTagFailed = new EventId(26005, "delete-tag-fail");
        public static EventId APIListTagSuccessful = new EventId(26006, "list-tag-success");
        public static EventId APIListTagFailed = new EventId(26007, "list-tag-fail");
        public static EventId APIDetailsTagSuccessful = new EventId(26008, "details-tag-success");
        public static EventId APIDetailsTagFailed = new EventId(26009, "details-tag-fail");
        public static EventId APIMergeTagSuccessful = new EventId(26010, "merge-tag-success");
        public static EventId APIMergeTagFailed = new EventId(26011, "merge-tag-fail");
        public static EventId APISubscribeTagSuccessful = new EventId(26012, "subscribe-tag-success");
        public static EventId APISubscribeTagFailed = new EventId(26013, "subscribe-tag-fail");
        public static EventId APIUnsubscribeTagSuccessful = new EventId(26012, "unsubscribe-tag-success");
        public static EventId APIUnsubscribeTagFailed = new EventId(26013, "unsubscribe-tag-fail");
        public static EventId APISearchTagSuccessful = new EventId(26014, "search-tag-success");
        public static EventId APISearchTagFailed = new EventId(26015, "search-tag-fail");

        public static EventId APICreateTagFacetSuccessful = new EventId(26014, "create-tagfacet-success");
        public static EventId APICreateTagFacetFailed = new EventId(26015, "create-tagfacet-fail");
        public static EventId APIEditTagFacetSuccessful = new EventId(26016, "edit-tagfacet-success");
        public static EventId APIEditTagFacetFailed = new EventId(26017, "edit-tagfacet-fail");
        public static EventId APIDeleteTagFacetSuccessful = new EventId(26018, "delete-tagfacet-success");
        public static EventId APIDeleteTagFacetFailed = new EventId(26019, "delete-tagfacet-fail");
        public static EventId APISubscribeSuccessful = new EventId(26020, "subscribe-tagfacet-success");
        public static EventId APISubscribeFacetFailed = new EventId(26021, "subscribe-tagfacet-fail");
        public static EventId APIUnsubscribeFacetSuccessful = new EventId(26022, "unsubscribe-tagfacet-success");
        public static EventId APIUnsubscribeFacetFailed = new EventId(26023, "unsubscribe-tagfacet-fail");
        public static EventId APIMergeFacetSuccessful = new EventId(26024, "merge-tagfacet-success");
        public static EventId APIMergeFacetFailed = new EventId(26025, "merge-tagfacet-fail");
        
        public static EventId APIListWhitelistFailed = new EventId(27000, "list-whitelist-fail");
        public static EventId APICreateWhitelistSucces = new EventId(27001, "create-whitelist-success");
        public static EventId APICreateWhitelistFailed = new EventId(27002, "create-whitelist-fail");
        public static EventId APIDeleteWhitelistSucces = new EventId(27003, "delete-whitelist-success");
        public static EventId APIDeleteWhitelistFailed = new EventId(27004, "delete-whitelist-fail");

        // Numbering schema:
        // First digit: 1 = WebApp, 2 = API
        // Second digit: 
        // 0 = Account
        // 1 = Comment
        // 2 = Import rule
        // 3 = Feed
        // 4 = Role
        // 5 = Source
        // 6 = Tag and/or Facet
        // 7 = User
        // 8 = Document

        private static EventId APIEventId(EventId eventId)
        {
            return new EventId(eventId.Id + 10000, "api-" + eventId.Name);
        }

        #region Account-related Event IDs

        public static EventId ForgotPasswordRequest = new EventId(10017, "forgot-password-fail");
        public static EventId ForgotPasswordSuccess = new EventId(10018, "forgot-password-success");
        public static EventId ForgotPasswordFailed = new EventId(10019, "forgot-password-fail");
        public static EventId EmailConfirmationRequest = new EventId(10020, "confirmation-link");
        public static EventId EmailConfirmationSuccess = new EventId(10021, "email-confirmation-success");
        public static EventId EmailConfirmationFailed = new EventId(10022, "email-confirmation-fail");
        public static EventId ResetPasswordRequest = new EventId(10023, "reset-password");
        public static EventId ResetPasswordFailed = new EventId(10024, "reset-password-fail");
        public static EventId ResetPasswordSuccess = new EventId(10025, "reset-password-success");
        public static EventId Disable2FARequest = new EventId(10026, "disable-2fa-request");
        public static EventId Disable2FAFailed = new EventId(10027, "disable-2fa-fail");
        public static EventId Disable2FASuccess = new EventId(10028, "disable-2fa-success");
        public static EventId Enable2FARequest = new EventId(10029, "enable-2fa-request");
        public static EventId Enable2FAFailed = new EventId(10030, "enable-2fa-fail");
        public static EventId Enable2FASuccess = new EventId(10031, "enable-2fa-success");
        public static EventId RecoveryCodeGenerated = new EventId(10032, "2fa-recovery-code-generated");
        
        #endregion

        #region Comment-related Event IDs

        #endregion

        #region Comment-related Event IDs

        public static EventId DiscardDocumentSuccessful = new EventId(18002, "discard-document-success");
        public static EventId DiscardDocumentFailed = new EventId(18003, "discard-document-fail");
        public static EventId PendingDocumentSuccessful = new EventId(18004, "pending-document-success");
        public static EventId UploadDocumentSuccessful = new EventId(18006, "upload-document-success");
        public static EventId UploadDocumentFailed = new EventId(18007, "upload-document-fail");
        public static EventId SubscribeDocumentSuccessful = new EventId(18008, "subscribe-document-success");
        public static EventId SubscribeDocumentFailed = new EventId(18009, "subscribe-document-fail");
        public static EventId UnsubscribeDocumentSuccessful = new EventId(18010, "unsubscribe-document-success");
        public static EventId UnsubscribeDocumentFailed = new EventId(18011, "unsubscribe-document-fail");
        public static EventId UpdateDocumentSuccessful = new EventId(18012, "update-document-success");
        public static EventId UpdateDocumentFailed = new EventId(18013, "update-document-fail");
        public static EventId DeleteDocumentSuccessful = new EventId(18014, "delete-document-success");
        public static EventId DeleteDocumentFailed = new EventId(18015, "delete-document-fail");
        public static EventId DetailsDocumentSuccessful = new EventId(18015, "details-document-success");
        public static EventId DetailsDocumentFailed = new EventId(18016, "details-document-fail");
        public static EventId RegisterDocumentSuccessful = new EventId(18015, "register-document-success");
        public static EventId RegisterDocumentFailed = new EventId(18016, "register-document-fail");

        public static EventId APIDiscardDocumentSuccessful = APIEventId(DiscardDocumentSuccessful);
        public static EventId APIDiscardDocumentFailed = APIEventId(DiscardDocumentFailed);
        public static EventId APIPendingDocumentSuccessful = APIEventId(PendingDocumentSuccessful);
        public static EventId APIUploadDocumentSuccessful = APIEventId(UploadDocumentSuccessful);
        public static EventId APIUploadDocumentFailed = APIEventId(UploadDocumentFailed);
        public static EventId APISubscribeDocumentSuccessful = APIEventId(SubscribeDocumentSuccessful);
        public static EventId APISubscribeDocumentFailed = APIEventId(SubscribeDocumentFailed);
        public static EventId APIUnsubscribeDocumentSuccessful = APIEventId(UnsubscribeDocumentSuccessful);
        public static EventId APIUnsubscribeDocumentFailed = APIEventId(UnsubscribeDocumentFailed);
        public static EventId APIUpdateDocumentSuccessful = APIEventId(UpdateDocumentSuccessful);
        public static EventId APIUpdateDocumentFailed = APIEventId(UpdateDocumentFailed);
        public static EventId APIDeleteDocumentSuccessful = APIEventId(DeleteDocumentSuccessful);
        public static EventId APIDeleteDocumentFailed = APIEventId(DeleteDocumentFailed);
        public static EventId APIDetailsDocumentSuccessful = APIEventId(DetailsDocumentSuccessful);
        public static EventId APIDetailsDocumentFailed = APIEventId(DetailsDocumentFailed);
        public static EventId APIRegisterDocumentSuccessful = APIEventId(RegisterDocumentSuccessful);
        public static EventId APIRegisterDocumentFailed = APIEventId(RegisterDocumentFailed);

        #endregion

        public static EventId LocalUserCreated = new EventId(1000, "local-user-created");
        public static EventId UserLogOnSuccess = new EventId(1001, "user-logon-success");
        public static EventId UserLogOff = new EventId(1002, "user-logoff");
        public static EventId UserLogOnDisabled = new EventId(1003, "user-logon-failed-disabled");
        public static EventId UserLogOnFailed = new EventId(1004, "user-logon-failed");
        public static EventId UserLogOnNotAllowed = new EventId(1005, "user-logon-failed-not-allowed");
        public static EventId Unauthorized = new(30001, "authorization-fail");
        public static EventId EntityNotFound = new(30002, "document-not-found");
        public static EventId CreateClassificationSuccessful = new(14000, "create-classification-success");
        public static EventId CreateClassificationFailed = new(14001, "create-classification-fail");
        public static EventId DeleteClassificationSuccessful = new(14002, "delete-classification-success");
        public static EventId DeleteClassificationFailed = new(14003, "delete-classification-fail");
        public static EventId EditClassificationSuccessful = new(14004, "edit-classification-success");
        public static EventId EditClassificationFailed = new(14005, "edit-classification-fail");
        public static EventId ListClassificationSuccessful = new(14006, "list-classification-success");
        public static EventId ListClassificationFailed = new(14007, "list-classification-fail");
        public static EventId DetailsClassificationSuccessful = new(14012, "details-classification-success");
        public static EventId DetailsClassificationFailed = new(14013, "details-classification-fail");
        public static EventId DownloadFailed = new EventId(18001, "download-document-fail");
        
        public static EventId ListFilesSuccessful = new(14006, "list-files-success");
        public static EventId ListFilesFailed = new(14007, "list-files-fail");
        public static EventId DetailsFileSuccessful = new(14006, "details-file-success");
        public static EventId DetailsFileFailed = new(14007, "details-file-fail");
        public static EventId UpdateFileSuccessful = new(14006, "update-file-success");
        public static EventId UpdateFileFailed = new(14007, "update-file-fail");
        public static EventId CreateFileSuccessful = new(14008, "create-file-success");
        public static EventId CreateFileFailed = new(14009, "create-file-fail");
        public static EventId DeleteFileSuccessful = new(14010, "delete-file-success");
        public static EventId DeleteFileFailed = new(14011, "delete-file-fail");

        public static EventId CreateGroupSuccessful = new EventId(14000, "create-group-success");
        public static EventId CreateGroupFailed = new EventId(14001, "create-group-fail");
        public static EventId DeleteGroupSuccessful = new EventId(14002, "delete-group-success");
        public static EventId DeleteGroupFailed = new EventId(14003, "delete-group-fail");
        public static EventId EditGroupSuccessful = new EventId(14004, "edit-group-success");
        public static EventId EditGroupFailed = new EventId(14005, "edit-group-fail");
        public static EventId ListGroupSuccessful = new EventId(14006, "list-group-success");
        public static EventId ListGroupFailed = new EventId(14007, "list-group-fail");
        public static EventId AddGroupUserSuccessful = new EventId(14008, "add-member-success");
        public static EventId AddGroupUserFailed = new EventId(14009, "add-member-fail");
        public static EventId RemoveGroupUserSuccessful = new EventId(14010, "remove-member-success");
        public static EventId RemoveGroupUserFailed = new EventId(14011, "remove-member-fail");
        public static EventId DetailsGroupSuccessful = new EventId(14012, "details-group-success");
        public static EventId DetailsGroupFailed = new EventId(14013, "details-group-fail");
        public static EventId ListImportRuleFailed = new EventId(12000, "list-import-rules-fail");
        public static EventId ListImportRuleSuccessful = new EventId(12001, "list-import-rules-success");
        public static EventId DetailsImportRuleFailed = new EventId(12008, "list-import-rules-fail");
        public static EventId DetailsImportRuleSuccessful = new EventId(12009, "list-import-rules-success");
        public static EventId CreateImportRuleFailed = new EventId(12002, "create-import-rules-fail");
        public static EventId CreateImportRuleSuccessful = new EventId(12003, "create-import-rules-success");
        public static EventId UpdateImportRuleFailed = new EventId(12004, "update-import-rules-fail");
        public static EventId UpdateImportRuleSuccessful = new EventId(12005, "update-import-rules-success");
        public static EventId DeleteImportRuleFailed = new EventId(12006, "delete-import-rules-fail");
        public static EventId DeleteImportRuleSuccessful = new EventId(12007, "delete-import-rules-success");
        public static EventId ListIncomingFeedSuccessful = new(13000, "list-incoming-feed-success");
        public static EventId ListIncomingFeedFailed = new(13001, "list-incoming-feed-fail");
        public static EventId DetailsIncomingFeedSuccessful = new(13002, "details-incoming-feed-success");
        public static EventId DetailsIncomingFeedFailed = new(13003, "details-incoming-feed-fail");
        public static EventId EditIncomingFeedSuccessful = new(13004, "edit-incoming-feed-success");
        public static EventId EditIncomingFeedFailed = new(13005, "edit-incoming-feed-fail");
        public static EventId EditIncomingFeedError = new(13005, "edit-incoming-feed-error");
        public static EventId CreateIncomingFeedSuccess = new(13006, "edit-incoming-feed-success");
        public static EventId CreateIncomingFeedFailed = new(13007, "edit-incoming-feed-fail");
        public static EventId CreateRoleSuccessful = new(14000, "create-role-success");
        public static EventId CreateRoleFailed = new(14001, "create-role-fail");
        public static EventId DeleteRoleSuccessful = new(14002, "delete-role-success");
        public static EventId DeleteRoleFailed = new(14003, "delete-role-fail");
        public static EventId EditRoleSuccessful = new(14004, "edit-role-success");
        public static EventId EditRoleFailed = new(14005, "edit-role-fail");
        public static EventId ListRoleSuccessful = new(14006, "list-role-success");
        public static EventId ListRoleFailed = new(14007, "list-role-fail");
        public static EventId AddRoleUserSuccessful = new(14008, "add-role-user-success");
        public static EventId AddRoleUserFailed = new(14009, "add-role-user-fail");
        public static EventId RemoveRoleUserSuccessful = new(14010, "remove-role-user-success");
        public static EventId RemoveRoleUserFailed = new(14011, "remove-role-user-fail");
        public static EventId DetailsRoleSuccessful = new(14012, "details-role-success");
        public static EventId DetailsRoleFailed = new(14013, "details-role-fail");
        public static EventId ListScraperSuccessful = new EventId(13000, "list-incoming-feed-success");
        public static EventId ListScraperFailed = new EventId(13001, "list-incoming-feed-fail");
        public static EventId DetailsScraperSuccessful = new EventId(13002, "details-incoming-feed-success");
        public static EventId DetailsScraperFailed = new EventId(13003, "details-incoming-feed-fail");
        public static EventId EditScraperSuccessful = new EventId(13004, "edit-incoming-feed-success");
        public static EventId EditScraperFailed = new EventId(13005, "edit-incoming-feed-fail");
        public static EventId EditScraperError = new EventId(13005, "edit-incoming-feed-error");
        public static EventId CreateScraperSuccess = new EventId(13006, "edit-incoming-feed-success");
        public static EventId CreateScraperFailed = new EventId(13007, "edit-incoming-feed-fail");
        public static EventId DeleteScraperSuccess = new EventId(13008, "delete-incoming-feed-success");
        public static EventId DeleteScraperFailed = new EventId(13009, "delete-incoming-feed-fail");
        public static EventId CreateSourceSuccessful = new EventId(15000, "create-source-success");
        public static EventId CreateSourceFailed = new EventId(15001, "create-source-fail");
        public static EventId EditSourceSuccessful = new EventId(15002, "edit-source-success");
        public static EventId EditSourceFailed = new EventId(15003, "edit-source-fail");
        public static EventId DeleteSourceSuccessful = new EventId(15004, "delete-source-success");
        public static EventId DeleteSourceFailed = new EventId(15005, "delete-source-fail");
        public static EventId ListSourceSuccessful = new EventId(15006, "list-source-success");
        public static EventId ListSourceFailed = new EventId(15007, "list-source-fail");
        public static EventId DetailsSourceSuccessful = new EventId(15008, "details-source-success");
        public static EventId DetailsSourceFailed = new EventId(15009, "details-source-fail");
        public static EventId MergeSourceSuccessful = new EventId(15010, "merge-source-success");
        public static EventId MergeSourceFailed = new EventId(15011, "merge-source-fail");
        public static EventId SubscribeSourceSuccessful = new EventId(15012, "subscribe-source-success");
        public static EventId SubscribeSourceFailed = new EventId(15013, "subscribe-source-fail");
        public static EventId LogoSourceSuccessful = new EventId(15014, "logo-source-success");
        public static EventId LogoSourceFailed = new EventId(15015, "logo-source-fail");
        public static EventId CreateTagSuccessful = new EventId(16000, "create-tag-success");
        public static EventId CreateTagFailed = new EventId(16001, "create-tag-fail");
        public static EventId EditTagSuccessful = new EventId(16002, "edit-tag-success");
        public static EventId EditTagFailed = new EventId(16003, "edit-tag-fail");
        public static EventId DeleteTagSuccessful = new EventId(16004, "delete-tag-success");
        public static EventId DeleteTagFailed = new EventId(16005, "delete-tag-fail");
        public static EventId ListTagSuccessful = new EventId(16006, "list-tag-success");
        public static EventId ListTagFailed = new EventId(16007, "list-tag-fail");
        public static EventId DetailsTagSuccessful = new EventId(16008, "details-tag-success");
        public static EventId DetailsTagFailed = new EventId(16009, "details-tag-fail");
        public static EventId MergeTagSuccessful = new EventId(16010, "merge-tag-success");
        public static EventId MergeTagFailed = new EventId(16011, "merge-tag-fail");
        public static EventId SubscribeTagSuccessful = new EventId(16012, "subscribe-tag-success");
        public static EventId SubscribeTagFailed = new EventId(16013, "subscribe-tag-fail");
        public static EventId CreateTagFacetSuccessful = new(16000, "create-tagfacet-success");
        public static EventId CreateTagFacetFailed = new(16001, "create-tagfacet-fail");
        public static EventId EditTagFacetSuccessful = new(16002, "edit-tagfacet-success");
        public static EventId EditTagFacetFailed = new(16003, "edit-tagfacet-fail");
        public static EventId DeleteTagFacetSuccessful = new(16004, "delete-tagfacet-success");
        public static EventId DeleteTagFacetFailed = new(16005, "delete-tagfacet-fail");
        public static EventId MergeSuccessful = new(16006, "merge-tagfacet-success");
        public static EventId MergeFailed = new(16007, "merge-tagfacet-fail");
        public static EventId GetFacetSuccessful = new(16008, "get-tagfacet-success");
        public static EventId GetFacetFailed = new(16009, "get-tagfacet-fail");

        public static EventId ListUserFailed = new EventId(17000, "list-user-fail");
        public static EventId ListUserSuccessful = new EventId(17001, "list-user-success");
        public static EventId ProfileUserFailed = new EventId(17002, "user-profile-fail");
        public static EventId ProfileUserSuccessful = new EventId(17003, "user-profile-success");
        public static EventId EditUserFailed = new EventId(17004, "edit-user-fail");
        public static EventId EditUserSuccessful = new EventId(17005, "edit-user-success");
        
        public static EventId PasswordChangeSuccessful = new EventId(17006, "user-change-password-success");
        public static EventId PasswordChangeFailed = new EventId(17006, "user-change-password-fail");
        
        public static EventId ListImportRuleSetFailed = new EventId(18000, "list-import-rule-set-fail");
        public static EventId ListImportRuleSetSuccessful = new EventId(18001, "list-import-rule-set-success");
        public static EventId DetailsImportRuleSetFailed = new EventId(18008, "list-import-rule-set-fail");
        public static EventId DetailsImportRuleSetSuccessful = new EventId(18009, "list-import-rule-set-success");
        public static EventId CreateImportRuleSetFailed = new EventId(18002, "create-import-rule-set-fail");
        public static EventId CreateImportRuleSetSuccessful = new EventId(18003, "create-import-rule-set-success");
        public static EventId UpdateImportRuleSetFailed = new EventId(18004, "update-import-rule-set-fail");
        public static EventId UpdateImportRuleSetSuccessful = new EventId(18005, "update-import-rule-set-success");
        public static EventId DeleteImportRuleSetFailed = new EventId(18006, "delete-import-rule-set-fail");
        public static EventId DeleteImportRuleSetSuccessful = new EventId(18007, "delete-import-rule-set-success");
        
        
        public static EventId ListObservableSuccess = new EventId(19001, "list-observable-success");
        public static EventId ListObservableFailed = new EventId(19002, "list-observable-failed");
        public static EventId DetailsObservableSuccess = new EventId(19003, "get-observable-success");
        public static EventId DetailsObservableFailed = new EventId(19004, "get-observable-failed");
        public static EventId CreateObservableSuccess = new EventId(19005, "create-observable-success");
        public static EventId CreateObservableFailed = new EventId(19006, "create-observable-failed");
        public static EventId ReferenceObservableSuccess = new EventId(19005, "reference-observable-success");
        public static EventId ReferenceObservableFailed = new EventId(19006, "reference-observable-failed");
        public static EventId DereferenceObservableSuccess = new EventId(19005, "dereference-observable-success");
        public static EventId DereferenceObservableFailed = new EventId(19006, "dereference-observable-failed");
    }
}