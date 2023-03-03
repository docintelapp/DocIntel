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

// 

using System;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;

using Spectre.Console;

namespace DocIntel.AdminConsole.Commands.Users
{
    public abstract class UserCommand<T> : DocIntelCommand<T> where T : UserCommandSettings
    {
        protected UserCommand(DocIntelContext context, AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings, UserManager<AppUser> userManager, AppRoleManager roleManager) 
            : base(context, userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
        }

        protected static string GetPassword(UserCommandSettings commandSettings)
        {
            var password = commandSettings.Password;
            if (string.IsNullOrEmpty(password) & !commandSettings.RandomPassword)
            {
                string passwordConfirmation;
                do
                {
                    password = AnsiConsole.Prompt(new TextPrompt<string>("Password:").Secret());
                    passwordConfirmation =
                        AnsiConsole.Prompt(new TextPrompt<string>("Password (confirmation):").Secret());
                    if (password != passwordConfirmation)
                        Console.WriteLine("Typed password differs. Please retype.\n");
                } while (password != passwordConfirmation);
            }

            if (commandSettings.RandomPassword)
            {
                password = UserHelper.GenerateRandomPassword();
                AnsiConsole.Render(new Markup($"Random password generated: '[bold]{password.EscapeMarkup()}[/]'\n"));
            }

            return password;
        }

        protected static string GetUserName(UserCommandSettings commandSettings)
        {
            return GetField(commandSettings, "Username", commandSettings.UserName);
        }
    }
}