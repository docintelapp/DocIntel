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

using System;
using System.ComponentModel.DataAnnotations;

namespace DocIntel.Core.Models
{
    public class UserPreferences
    {
        private UIPreferences _ui;
        private DigestPreferences _digest;

        public UserPreferences()
        {
        }

        public UIPreferences UI
        {
            get => _ui;
            set => _ui = value;
        }

        public DigestPreferences Digest
        {
            get => _digest ;
            set => _digest = value;
        }

        public class DigestPreferences
        {
            private DigestFrequency _frequency;
            private DateTime _lastDigest;

            public enum DigestFrequency
            {
                None,
                Daily
            }

            [Display(Name = "Email Digests")]
            public DigestFrequency Frequency
            {
                get => _frequency;
                set => _frequency = value;
            }

            public DateTime LastDigest
            {
                get => _lastDigest;
                set => _lastDigest = value;
            }
        }

        public class UIPreferences
        {
            private string _timeZone;
            private UITheme _theme;
            private UIFontSize _fontSize = UIFontSize.Medium;
            private bool _biggerContentFont = false;
            private bool _highContrastText = false;

            public enum UIFontSize
            {
                Small,
                Medium,
                Large,
                ExtraLarge
            }

            public enum UITheme
            {
                Default,
                Light,
                Dark
            }

            [Display(Name = "TimeZone")]
            public string TimeZone
            {
                get => string.IsNullOrEmpty(_timeZone)
                    ? TimeZoneInfo.Local.StandardName
                    : _timeZone;
                set => _timeZone = value;
            }

            [Display(Name = "Theme")]
            public UITheme Theme
            {
                get => _theme;
                set => _theme = value;
            }

            [Display(Name = "Font size")]
            public UIFontSize FontSize
            {
                get => _fontSize;
                set => _fontSize = value;
            }

            [Display(Name = "Bigger Content Font")]
            public bool BiggerContentFont
            {
                get => _biggerContentFont;
                set => _biggerContentFont = value;
            }

            [Display(Name = "High Contrast Text (WCAG 2 AA)")]
            public bool HighContrastText
            {
                get => _highContrastText;
                set => _highContrastText = value;
            }
        }
    }
}