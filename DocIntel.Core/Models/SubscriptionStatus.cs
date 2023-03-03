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

namespace DocIntel.Core.Models
{
    /// <summary>
    ///     Represents the state of the subscription. A user can subscribe to elements, with or without being notified
    ///     of the events related to the subscription. The behaviour is the same as the one applied to YouTube.
    /// </summary>
    public class SubscriptionStatus
    {
        public SubscriptionStatus()
        {
            Notification = false;
        }

        /// <summary>
        ///     Whether the user is subscribed.
        /// </summary>
        /// <value><c>True</c> if the user is subscribed, <c>False</c> otherwise.</value>
        public bool Subscribed { get; set; }

        /// <summary>
        ///     Whether the user want to be notified of changes.
        /// </summary>
        /// <value><c>True</c> if the user wants to be notified, <c>False</c> otherwise.</value>
        public bool Notification { get; set; }
    }
}