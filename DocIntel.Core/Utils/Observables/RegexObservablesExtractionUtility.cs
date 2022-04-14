/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau, Kevin Menten
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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Utils.Observables
{
    public class RegexObservablesExtractionUtility : IObservablesExtractionUtility
    {
        public const string REGEX_URL_TEMPLATE = @"//([A-Za-z0–9-]{{1,63}}\.)*{0}[/|$]";
        public const string REGEX_FQDN_TEMPLATE = @"^([A-Za-z0–9-]{{1,63}}\.)*{0}$";

        public const string END_PUNCTUATION = @"[\.\?>""'\)!,}:;\u201d\u2019\uff1e\uff1c\]]*";

        public const string SEPARATOR_DEFANGS = @"[\(\)\[\]{}<>\\]";

        public const string DEFANGED_DOT_REGEX = @"\.|\[\.\]|\(\.\)|\sDOT\s|\[dot\]|\[DOT\]|\(dot\)|\(DOT\)";

        // Currently not matching unicode characters, due to high false positive rates when auto-extracting
        public const string REGEX_DOMAIN_REGEX = @"
            # see if preceded by slashes or @
            (\/|\\|@|@\]|%2F)? 
            (           
            (?:  
                [a-zA-Z\d-]{1,63}  # Alphanumeric chunk (also dashes)
            (?:\.|" + DEFANGED_DOT_REGEX + @")             # Dot separator between labels.
                ){1,63}
            
                [a-zA-Z]{2,}  # Top level domain (numbers excluded)
            )
            ";

        public const string GENERIC_URL_REGEX = @"
        (
            # Scheme.
            [fhstu]\S\S?[px]s?
            # One of these delimiters/defangs.
            (?:
                :\/\/|
                :\\\\|
                :?__
            )
            # Any number of defang characters.
            (?:
                \x20|" + SEPARATOR_DEFANGS + @"
            )*
            # Domain/path characters.
            [a-zA-Z0-9:\./_~!$&'()*+,;=:@-]+?
            # CISCO ESA style defangs followed by domain/path characters.
            (?:\x20[\/\.][^\.\/\s]\S*?)*
        )" + END_PUNCTUATION + @"(?=[\s""<>^`{|}]|$)";
        
        public const string BRACKET_URL_REGEX = @"
            \b
            (
                [\.\:\/\\\w\[\]\(\)-]+
                (?:
                    \x20?
                    [\(\[]
                    \x20?
                    \.
                    \x20?
                    [\]\)]
                    \x20?
                    [a-zA-Z0-9\.-_~!$&'()*+,;=:@]*?
                )+
            )
        " + END_PUNCTUATION + @"
            (?=\s|$)
        ";

        // REVIEW Suspiciously unused field
        // ReSharper disable once UnusedMember.Local
        public const string BACKSLASH_URL_REGEX = @"
            \b
            (
                [\:\/\\\w\[\]\(\)-]+
                (?:
                    \x20?
                    \\?\.
                    \x20?
                    \S*?
                )*?
                (?:
                    \x20?
                    \\\.
                    \x20?
                    \S*?
                )
                (?:
                    \x20?
                    \\?\.
                    \x20?
                    \S*?
                )*
            )
        " + END_PUNCTUATION + @"
            (?=\s|$)
        ";

        public const string MD5_REGEX = @"[^a-fA-F\d\/=\\]([a-fA-F\d]{32})[^a-fA-F\d\/=\\]";
        public const string SHA1_REGEX = @"[^a-fA-F\d\/=\\]([a-fA-F\d]{40})[^a-fA-F\d\/=\\]";
        public const string SHA256_REGEX = @"[^a-fA-F\d\/=\\]([a-fA-F\d]{64})[^a-fA-F\d\/=\\]";
        public const string SHA512_REGEX = @"[^a-fA-F\d\/=\\]([a-fA-F\d]{128})[^a-fA-F\d\/=\\]";

        public const string IPV4_REGEX = @"
            (?:^|
                (?![^\d\.])
            )
            (?:
                (?:[1-9]?\d|1\d\d|2[0-4]\d|25[0-5])
                [\[\(\\]*?\.[\]\)]*?
            ){3}
            (?:[1-9]?\d|1\d\d|2[0-4]\d|25[0-5])
            (?:(?=[^\d\.])|$)
        ";

        // TODO The following regex generates a too high false positive rate.
        // ReSharper disable once UnusedMember.Local
        public const string IPV6_REGEX = @"
            \b(?:[a-f0-9]{1,4}:|:){2,7}(?:[a-f0-9]{1,4}|:)\b
        ";
        
        // TODO Probably best to have this NOT compiled within the application but available as a custom option
        // TODO Extend to all known and common file extensions
        private static readonly string[] _TLDToReview =
        {
            "PF", "XXX", "PY", "ZIP", "VI", "STREAM", "OPEN", "AI", "CAB", "PROPERTIES", "JAVA", "SHELL", "RUN", "SAVE",
            "SH", "TT", "PS", "APP", "READ", "PLUS", "DATA", "PE", "ANDROID", "AS", "INC", "IO", "MANAGEMENT", "MOV",
            "NAME", "NEW", "NOW", "SEEK", "SERVICES", "WIN", "SO"
        };

        // TODO Probably best to have this NOT compiled within the application but available as a custom option 
        private static readonly string[] _TLD =
        {
            "AAA", "AARP", "ABARTH", "ABB", "ABBOTT", "ABBVIE", "ABC", "ABLE", "ABOGADO", "ABUDHABI", "AC", "ACADEMY",
            "ACCENTURE", "ACCOUNTANT", "ACCOUNTANTS", "ACO", "ACTOR", "AD", "ADAC", "ADS", "ADULT", "AE", "AEG", "AERO",
            "AETNA", "AF", "AFAMILYCOMPANY", "AFL", "AFRICA", "AG", "AGAKHAN", "AGENCY", "AI", "AIG", "AIRBUS",
            "AIRFORCE", "AIRTEL", "AKDN", "AL", "ALFAROMEO", "ALIBABA", "ALIPAY", "ALLFINANZ", "ALLSTATE", "ALLY",
            "ALSACE", "ALSTOM", "AM", "AMAZON", "AMERICANEXPRESS", "AMERICANFAMILY", "AMEX", "AMFAM", "AMICA",
            "AMSTERDAM", "ANALYTICS", "ANDROID", "ANQUAN", "ANZ", "AO", "AOL", "APARTMENTS", "APP", "APPLE", "AQ",
            "AQUARELLE", "AR", "ARAB", "ARAMCO", "ARCHI", "ARMY", "ARPA", "ART", "ARTE", "AS", "ASDA", "ASIA",
            "ASSOCIATES", "AT", "ATHLETA", "ATTORNEY", "AU", "AUCTION", "AUDI", "AUDIBLE", "AUDIO", "AUSPOST", "AUTHOR",
            "AUTO", "AUTOS", "AVIANCA", "AW", "AWS", "AX", "AXA", "AZ", "AZURE", "BA", "BABY", "BAIDU", "BANAMEX",
            "BANANAREPUBLIC", "BAND", "BANK", "BAR", "BARCELONA", "BARCLAYCARD", "BARCLAYS", "BAREFOOT", "BARGAINS",
            "BASEBALL", "BASKETBALL", "BAUHAUS", "BAYERN", "BB", "BBC", "BBT", "BBVA", "BCG", "BCN", "BD", "BE",
            "BEATS", "BEAUTY", "BEER", "BENTLEY", "BERLIN", "BEST", "BESTBUY", "BET", "BF", "BG", "BH", "BHARTI", "BI",
            "BIBLE", "BID", "BIKE", "BING", "BINGO", "BIO", "BIZ", "BJ", "BLACK", "BLACKFRIDAY", "BLOCKBUSTER", "BLOG",
            "BLOOMBERG", "BLUE", "BM", "BMS", "BMW", "BN", "BNPPARIBAS", "BO", "BOATS", "BOEHRINGER", "BOFA", "BOM",
            "BOND", "BOO", "BOOK", "BOOKING", "BOSCH", "BOSTIK", "BOSTON", "BOT", "BOUTIQUE", "BOX", "BR", "BRADESCO",
            "BRIDGESTONE", "BROADWAY", "BROKER", "BROTHER", "BRUSSELS", "BS", "BT", "BUDAPEST", "BUGATTI", "BUILD",
            "BUILDERS", "BUSINESS", "BUY", "BUZZ", "BV", "BW", "BY", "BZ", "BZH", "CA", "CAB", "CAFE", "CAL", "CALL",
            "CALVINKLEIN", "CAM", "CAMERA", "CAMP", "CANCERRESEARCH", "CANON", "CAPETOWN", "CAPITAL", "CAPITALONE",
            "CAR", "CARAVAN", "CARDS", "CARE", "CAREER", "CAREERS", "CARS", "CASA", "CASE", "CASH", "CASINO", "CAT",
            "CATERING", "CATHOLIC", "CBA", "CBN", "CBRE", "CBS", "CC", "CD", "CENTER", "CEO", "CERN", "CF", "CFA",
            "CFD", "CG", "CH", "CHANEL", "CHANNEL", "CHARITY", "CHASE", "CHAT", "CHEAP", "CHINTAI", "CHRISTMAS",
            "CHROME", "CHURCH", "CI", "CIPRIANI", "CIRCLE", "CISCO", "CITADEL", "CITI", "CITIC", "CITY", "CITYEATS",
            "CK", "CL", "CLAIMS", "CLEANING", "CLICK", "CLINIC", "CLINIQUE", "CLOTHING", "CLOUD", "CLUB", "CLUBMED",
            "CM", "CN", "CO", "COACH", "CODES", "COFFEE", "COLLEGE", "COLOGNE", "COM", "COMCAST", "COMMBANK",
            "COMMUNITY", "COMPANY", "COMPARE", "COMPUTER", "COMSEC", "CONDOS", "CONSTRUCTION", "CONSULTING", "CONTACT",
            "CONTRACTORS", "COOKING", "COOKINGCHANNEL", "COOL", "COOP", "CORSICA", "COUNTRY", "COUPON", "COUPONS",
            "COURSES", "CPA", "CR", "CREDIT", "CREDITCARD", "CREDITUNION", "CRICKET", "CROWN", "CRS", "CRUISE",
            "CRUISES", "CSC", "CU", "CUISINELLA", "CV", "CW", "CX", "CY", "CYMRU", "CYOU", "CZ", "DABUR", "DAD",
            "DANCE", "DATA", "DATE", "DATING", "DATSUN", "DAY", "DCLK", "DDS", "DE", "DEAL", "DEALER", "DEALS",
            "DEGREE", "DELIVERY", "DELL", "DELOITTE", "DELTA", "DEMOCRAT", "DENTAL", "DENTIST", "DESI", "DESIGN", "DEV",
            "DHL", "DIAMONDS", "DIET", "DIGITAL", "DIRECT", "DIRECTORY", "DISCOUNT", "DISCOVER", "DISH", "DIY", "DJ",
            "DK", "DM", "DNP", "DO", "DOCS", "DOCTOR", "DOG", "DOMAINS", "DOT", "DOWNLOAD", "DRIVE", "DTV", "DUBAI",
            "DUCK", "DUNLOP", "DUPONT", "DURBAN", "DVAG", "DVR", "DZ", "EARTH", "EAT", "EC", "ECO", "EDEKA", "EDU",
            "EDUCATION", "EE", "EG", "EMAIL", "EMERCK", "ENERGY", "ENGINEER", "ENGINEERING", "ENTERPRISES", "EPSON",
            "EQUIPMENT", "ER", "ERICSSON", "ERNI", "ES", "ESQ", "ESTATE", "ET", "ETISALAT", "EU", "EUROVISION", "EUS",
            "EVENTS", "EXCHANGE", "EXPERT", "EXPOSED", "EXPRESS", "EXTRASPACE", "FAGE", "FAIL", "FAIRWINDS", "FAITH",
            "FAMILY", "FAN", "FANS", "FARM", "FARMERS", "FASHION", "FAST", "FEDEX", "FEEDBACK", "FERRARI", "FERRERO",
            "FI", "FIAT", "FIDELITY", "FIDO", "FILM", "FINAL", "FINANCE", "FINANCIAL", "FIRE", "FIRESTONE", "FIRMDALE",
            "FISH", "FISHING", "FIT", "FITNESS", "FJ", "FK", "FLICKR", "FLIGHTS", "FLIR", "FLORIST", "FLOWERS", "FLY",
            "FM", "FO", "FOO", "FOOD", "FOODNETWORK", "FOOTBALL", "FORD", "FOREX", "FORSALE", "FORUM", "FOUNDATION",
            "FOX", "FR", "FREE", "FRESENIUS", "FRL", "FROGANS", "FRONTDOOR", "FRONTIER", "FTR", "FUJITSU", "FUN",
            "FUND", "FURNITURE", "FUTBOL", "FYI", "GA", "GAL", "GALLERY", "GALLO", "GALLUP", "GAME", "GAMES", "GAP",
            "GARDEN", "GAY", "GB", "GBIZ", "GD", "GDN", "GE", "GEA", "GENT", "GENTING", "GEORGE", "GF", "GG", "GGEE",
            "GH", "GI", "GIFT", "GIFTS", "GIVES", "GIVING", "GL", "GLADE", "GLASS", "GLE", "GLOBAL", "GLOBO", "GM",
            "GMAIL", "GMBH", "GMO", "GMX", "GN", "GODADDY", "GOLD", "GOLDPOINT", "GOLF", "GOO", "GOODYEAR", "GOOG",
            "GOOGLE", "GOP", "GOT", "GOV", "GP", "GQ", "GR", "GRAINGER", "GRAPHICS", "GRATIS", "GREEN", "GRIPE",
            "GROCERY", "GROUP", "GS", "GT", "GU", "GUARDIAN", "GUCCI", "GUGE", "GUIDE", "GUITARS", "GURU", "GW", "GY",
            "HAIR", "HAMBURG", "HANGOUT", "HAUS", "HBO", "HDFC", "HDFCBANK", "HEALTH", "HEALTHCARE", "HELP", "HELSINKI",
            "HERE", "HERMES", "HGTV", "HIPHOP", "HISAMITSU", "HITACHI", "HIV", "HK", "HKT", "HM", "HN", "HOCKEY",
            "HOLDINGS", "HOLIDAY", "HOMEDEPOT", "HOMEGOODS", "HOMES", "HOMESENSE", "HONDA", "HORSE", "HOSPITAL", "HOST",
            "HOSTING", "HOT", "HOTELES", "HOTELS", "HOTMAIL", "HOUSE", "HOW", "HR", "HSBC", "HT", "HU", "HUGHES",
            "HYATT", "HYUNDAI", "IBM", "ICBC", "ICE", "ICU", "ID", "IE", "IEEE", "IFM", "IKANO", "IL", "IM", "IMAMAT",
            "IMDB", "IMMO", "IMMOBILIEN", "IN", "INC", "INDUSTRIES", "INFINITI", "INFO", "ING", "INK", "INSTITUTE",
            "INSURANCE", "INSURE", "INT", "INTERNATIONAL", "INTUIT", "INVESTMENTS", "IO", "IPIRANGA", "IQ", "IR",
            "IRISH", "IS", "ISMAILI", "IST", "ISTANBUL", "IT", "ITAU", "ITV", "JAGUAR", "JAVA", "JCB", "JE", "JEEP",
            "JETZT", "JEWELRY", "JIO", "JLL", "JM", "JMP", "JNJ", "JO", "JOBS", "JOBURG", "JOT", "JOY", "JP",
            "JPMORGAN", "JPRS", "JUEGOS", "JUNIPER", "KAUFEN", "KDDI", "KE", "KERRYHOTELS", "KERRYLOGISTICS",
            "KERRYPROPERTIES", "KFH", "KG", "KH", "KI", "KIA", "KIM", "KINDER", "KINDLE", "KITCHEN", "KIWI", "KM", "KN",
            "KOELN", "KOMATSU", "KOSHER", "KP", "KPMG", "KPN", "KR", "KRD", "KRED", "KUOKGROUP", "KW", "KY", "KYOTO",
            "KZ", "LA", "LACAIXA", "LAMBORGHINI", "LAMER", "LANCASTER", "LANCIA", "LAND", "LANDROVER", "LANXESS",
            "LASALLE", "LAT", "LATINO", "LATROBE", "LAW", "LAWYER", "LB", "LC", "LDS", "LEASE", "LECLERC", "LEFRAK",
            "LEGAL", "LEGO", "LEXUS", "LGBT", "LI", "LIDL", "LIFE", "LIFEINSURANCE", "LIFESTYLE", "LIGHTING", "LIKE",
            "LILLY", "LIMITED", "LIMO", "LINCOLN", "LINDE", "LINK", "LIPSY", "LIVE", "LIVING", "LIXIL", "LK", "LLC",
            "LLP", "LOAN", "LOANS", "LOCKER", "LOCUS", "LOFT", "LOL", "LONDON", "LOTTE", "LOTTO", "LOVE", "LPL",
            "LPLFINANCIAL", "LR", "LS", "LT", "LTD", "LTDA", "LU", "LUNDBECK", "LUXE", "LUXURY", "LV", "LY", "MA",
            "MACYS", "MADRID", "MAIF", "MAISON", "MAKEUP", "MAN", "MANAGEMENT", "MANGO", "MAP", "MARKET", "MARKETING",
            "MARKETS", "MARRIOTT", "MARSHALLS", "MASERATI", "MATTEL", "MBA", "MC", "MCKINSEY", "MD", "ME", "MED",
            "MEDIA", "MEET", "MELBOURNE", "MEME", "MEMORIAL", "MEN", "MENU", "MERCKMSD", "MG", "MH", "MIAMI",
            "MICROSOFT", "MIL", "MINI", "MINT", "MIT", "MITSUBISHI", "MK", "ML", "MLB", "MLS", "MM", "MMA", "MN", "MO",
            "MOBI", "MOBILE", "MODA", "MOE", "MOI", "MOM", "MONASH", "MONEY", "MONSTER", "MORMON", "MORTGAGE", "MOSCOW",
            "MOTO", "MOTORCYCLES", "MOV", "MOVIE", "MP", "MQ", "MR", "MS", "MSD", "MT", "MTN", "MTR", "MU", "MUSEUM",
            "MUTUAL", "MV", "MW", "MX", "MY", "MZ", "NA", "NAB", "NAGOYA", "NAME", "NATURA", "NAVY", "NBA", "NC", "NE",
            "NEC", "NET", "NETBANK", "NETFLIX", "NETWORK", "NEUSTAR", "NEW", "NEWS", "NEXT", "NEXTDIRECT", "NEXUS",
            "NF", "NFL", "NG", "NGO", "NHK", "NI", "NICO", "NIKE", "NIKON", "NINJA", "NISSAN", "NISSAY", "NL", "NO",
            "NOKIA", "NORTHWESTERNMUTUAL", "NORTON", "NOW", "NOWRUZ", "NOWTV", "NP", "NR", "NRA", "NRW", "NTT", "NU",
            "NYC", "NZ", "OBI", "OBSERVER", "OFF", "OFFICE", "OKINAWA", "OLAYAN", "OLAYANGROUP", "OLDNAVY", "OLLO",
            "OM", "OMEGA", "ONE", "ONG", "ONL", "ONLINE", "OOO", "OPEN", "ORACLE", "ORANGE", "ORG", "ORGANIC",
            "ORIGINS", "OSAKA", "OTSUKA", "OTT", "OVH", "PA", "PAGE", "PANASONIC", "PARIS", "PARS", "PARTNERS", "PARTS",
            "PARTY", "PASSAGENS", "PAY", "PCCW", "PE", "PET", "PF", "PFIZER", "PG", "PH", "PHARMACY", "PHD", "PHILIPS",
            "PHONE", "PHOTO", "PHOTOGRAPHY", "PHOTOS", "PHYSIO", "PICS", "PICTET", "PICTURES", "PID", "PIN", "PING",
            "PINK", "PIONEER", "PIZZA", "PK", "PL", "PLACE", "PLAY", "PLAYSTATION", "PLUMBING", "PLUS", "PM", "PN",
            "PNC", "POHL", "POKER", "POLITIE", "PORN", "POST", "PR", "PRAMERICA", "PRAXI", "PRESS", "PRIME", "PRO",
            "PROD", "PRODUCTIONS", "PROF", "PROGRESSIVE", "PROMO", "PROPERTIES", "PROPERTY", "PROTECTION", "PRU",
            "PRUDENTIAL", "PS", "PT", "PUB", "PW", "PWC", "PY", "QA", "QPON", "QUEBEC", "QUEST", "QVC", "RACING",
            "RADIO", "RAID", "RE", "READ", "REALESTATE", "REALTOR", "REALTY", "RECIPES", "RED", "REDSTONE",
            "REDUMBRELLA", "REHAB", "REISE", "REISEN", "REIT", "RELIANCE", "REN", "RENT", "RENTALS", "REPAIR", "REPORT",
            "REPUBLICAN", "REST", "RESTAURANT", "REVIEW", "REVIEWS", "REXROTH", "RICH", "RICHARDLI", "RICOH", "RIL",
            "RIO", "RIP", "RMIT", "RO", "ROCHER", "ROCKS", "RODEO", "ROGERS", "ROOM", "RS", "RSVP", "RU", "RUGBY",
            "RUHR", "RUN", "RW", "RWE", "RYUKYU", "SA", "SAARLAND", "SAFE", "SAFETY", "SAKURA", "SALE", "SALON",
            "SAMSCLUB", "SAMSUNG", "SANDVIK", "SANDVIKCOROMANT", "SANOFI", "SAP", "SARL", "SAS", "SAVE", "SAXO", "SB",
            "SBI", "SBS", "SC", "SCA", "SCB", "SCHAEFFLER", "SCHMIDT", "SCHOLARSHIPS", "SCHOOL", "SCHULE", "SCHWARZ",
            "SCIENCE", "SCJOHNSON", "SCOT", "SD", "SE", "SEARCH", "SEAT", "SECURE", "SECURITY", "SEEK", "SELECT",
            "SENER", "SERVICES", "SES", "SEVEN", "SEW", "SEX", "SEXY", "SFR", "SG", "SH", "SHANGRILA", "SHARP", "SHAW",
            "SHELL", "SHIA", "SHIKSHA", "SHOES", "SHOP", "SHOPPING", "SHOUJI", "SHOW", "SHOWTIME", "SI", "SILK", "SINA",
            "SINGLES", "SITE", "SJ", "SK", "SKI", "SKIN", "SKY", "SKYPE", "SL", "SLING", "SM", "SMART", "SMILE", "SN",
            "SNCF", "SO", "SOCCER", "SOCIAL", "SOFTBANK", "SOFTWARE", "SOHU", "SOLAR", "SOLUTIONS", "SONG", "SONY",
            "SOY", "SPA", "SPACE", "SPORT", "SPOT", "SR", "SRL", "SS", "ST", "STADA", "STAPLES", "STAR", "STATEBANK",
            "STATEFARM", "STC", "STCGROUP", "STOCKHOLM", "STORAGE", "STORE", "STREAM", "STUDIO", "STUDY", "STYLE", "SU",
            "SUCKS", "SUPPLIES", "SUPPLY", "SUPPORT", "SURF", "SURGERY", "SUZUKI", "SV", "SWATCH", "SWIFTCOVER",
            "SWISS", "SX", "SY", "SYDNEY", "SYSTEMS", "SZ", "TAB", "TAIPEI", "TALK", "TAOBAO", "TARGET", "TATAMOTORS",
            "TATAR", "TATTOO", "TAX", "TAXI", "TC", "TCI", "TD", "TDK", "TEAM", "TECH", "TECHNOLOGY", "TEL", "TEMASEK",
            "TENNIS", "TEVA", "TF", "TG", "TH", "THD", "THEATER", "THEATRE", "TIAA", "TICKETS", "TIENDA", "TIFFANY",
            "TIPS", "TIRES", "TIROL", "TJ", "TJMAXX", "TJX", "TK", "TKMAXX", "TL", "TM", "TMALL", "TN", "TO", "TODAY",
            "TOKYO", "TOOLS", "TOP", "TORAY", "TOSHIBA", "TOTAL", "TOURS", "TOWN", "TOYOTA", "TOYS", "TR", "TRADE",
            "TRADING", "TRAINING", "TRAVEL", "TRAVELCHANNEL", "TRAVELERS", "TRAVELERSINSURANCE", "TRUST", "TRV", "TT",
            "TUBE", "TUI", "TUNES", "TUSHU", "TV", "TVS", "TW", "TZ", "UA", "UBANK", "UBS", "UG", "UK", "UNICOM",
            "UNIVERSITY", "UNO", "UOL", "UPS", "US", "UY", "UZ", "VA", "VACATIONS", "VANA", "VANGUARD", "VC", "VE",
            "VEGAS", "VENTURES", "VERISIGN", "VERSICHERUNG", "VET", "VG", "VI", "VIAJES", "VIDEO", "VIG", "VIKING",
            "VILLAS", "VIN", "VIP", "VIRGIN", "VISA", "VISION", "VIVA", "VIVO", "VLAANDEREN", "VN", "VODKA",
            "VOLKSWAGEN", "VOLVO", "VOTE", "VOTING", "VOTO", "VOYAGE", "VU", "VUELOS", "WALES", "WALMART", "WALTER",
            "WANG", "WANGGOU", "WATCH", "WATCHES", "WEATHER", "WEATHERCHANNEL", "WEBCAM", "WEBER", "WEBSITE", "WED",
            "WEDDING", "WEIBO", "WEIR", "WF", "WHOSWHO", "WIEN", "WIKI", "WILLIAMHILL", "WIN", "WINDOWS", "WINE",
            "WINNERS", "WME", "WOLTERSKLUWER", "WOODSIDE", "WORK", "WORKS", "WORLD", "WOW", "WS", "WTC", "WTF", "XBOX",
            "XEROX", "XFINITY", "XIHUAN", "XIN", "XN--11B4C3D", "XN--1CK2E1B", "XN--1QQW23A", "XN--2SCRJ9C",
            "XN--30RR7Y", "XN--3BST00M", "XN--3DS443G", "XN--3E0B707E", "XN--3HCRJ9C", "XN--3OQ18VL8PN36A",
            "XN--3PXU8K", "XN--42C2D9A", "XN--45BR5CYL", "XN--45BRJ9C", "XN--45Q11C", "XN--4DBRK0CE", "XN--4GBRIM",
            "XN--54B7FTA0CC", "XN--55QW42G", "XN--55QX5D", "XN--5SU34J936BGSG", "XN--5TZM5G", "XN--6FRZ82G",
            "XN--6QQ986B3XL", "XN--80ADXHKS", "XN--80AO21A", "XN--80AQECDR1A", "XN--80ASEHDB", "XN--80ASWG",
            "XN--8Y0A063A", "XN--90A3AC", "XN--90AE", "XN--90AIS", "XN--9DBQ2A", "XN--9ET52U", "XN--9KRT00A",
            "XN--B4W605FERD", "XN--BCK1B9A5DRE4C", "XN--C1AVG", "XN--C2BR7G", "XN--CCK2B3B", "XN--CCKWCXETD",
            "XN--CG4BKI", "XN--CLCHC0EA0B2G2A9GCD", "XN--CZR694B", "XN--CZRS0T", "XN--CZRU2D", "XN--D1ACJ3B",
            "XN--D1ALF", "XN--E1A4C", "XN--ECKVDTC9D", "XN--EFVY88H", "XN--FCT429K", "XN--FHBEI", "XN--FIQ228C5HS",
            "XN--FIQ64B", "XN--FIQS8S", "XN--FIQZ9S", "XN--FJQ720A", "XN--FLW351E", "XN--FPCRJ9C3D", "XN--FZC2C9E2C",
            "XN--FZYS8D69UVGM", "XN--G2XX48C", "XN--GCKR3F0F", "XN--GECRJ9C", "XN--GK3AT1E", "XN--H2BREG3EVE",
            "XN--H2BRJ9C", "XN--H2BRJ9C8C", "XN--HXT814E", "XN--I1B6B1A6A2E", "XN--IMR513N", "XN--IO0A7I", "XN--J1AEF",
            "XN--J1AMH", "XN--J6W193G", "XN--JLQ480N2RG", "XN--JLQ61U9W7B", "XN--JVR189M", "XN--KCRX77D1X4A",
            "XN--KPRW13D", "XN--KPRY57D", "XN--KPUT3I", "XN--L1ACC", "XN--LGBBAT1AD8J", "XN--MGB9AWBF",
            "XN--MGBA3A3EJT", "XN--MGBA3A4F16A", "XN--MGBA7C0BBN0A", "XN--MGBAAKC7DVF", "XN--MGBAAM7A8H",
            "XN--MGBAB2BD", "XN--MGBAH1A3HJKRD", "XN--MGBAI9AZGQP6J", "XN--MGBAYH7GPA", "XN--MGBBH1A", "XN--MGBBH1A71E",
            "XN--MGBC0A9AZCG", "XN--MGBCA7DZDO", "XN--MGBCPQ6GPA1A", "XN--MGBERP4A5D4AR", "XN--MGBGU82A",
            "XN--MGBI4ECEXP", "XN--MGBPL2FH", "XN--MGBT3DHD", "XN--MGBTX2B", "XN--MGBX4CD0AB", "XN--MIX891F",
            "XN--MK1BU44C", "XN--MXTQ1M", "XN--NGBC5AZD", "XN--NGBE9E0A", "XN--NGBRX", "XN--NODE", "XN--NQV7F",
            "XN--NQV7FS00EMA", "XN--NYQY26A", "XN--O3CW4H", "XN--OGBPF8FL", "XN--OTU796D", "XN--P1ACF", "XN--P1AI",
            "XN--PGBS0DH", "XN--PSSY2U", "XN--Q7CE6A", "XN--Q9JYB4C", "XN--QCKA1PMC", "XN--QXA6A", "XN--QXAM",
            "XN--RHQV96G", "XN--ROVU88B", "XN--RVC1E0AM3E", "XN--S9BRJ9C", "XN--SES554G", "XN--T60B56A", "XN--TCKWE",
            "XN--TIQ49XQYJ", "XN--UNUP4Y", "XN--VERMGENSBERATER-CTB", "XN--VERMGENSBERATUNG-PWB", "XN--VHQUV",
            "XN--VUQ861B", "XN--W4R85EL8FHU5DNRA", "XN--W4RS40L", "XN--WGBH1C", "XN--WGBL6A", "XN--XHQ521B",
            "XN--XKC2AL3HYE2A", "XN--XKC2DL3A5EE0H", "XN--Y9A3AQ", "XN--YFRO4I67O", "XN--YGBI2AMMX", "XN--ZFR164B",
            "XXX", "XYZ", "YACHTS", "YAHOO", "YAMAXUN", "YANDEX", "YE", "YODOBASHI", "YOGA", "YOKOHAMA", "YOU",
            "YOUTUBE", "YT", "YUN", "ZA", "ZAPPOS", "ZARA", "ZERO", "ZIP", "ZM", "ZONE", "ZUERICH", "ZW"
        };

        private readonly IObservableRepository _observableRepository;
        private readonly ILogger<RegexObservablesExtractionUtility> _logger;
        private readonly IObservableWhitelistUtility _observableWhitelistUtility;

        private const RegexOptions DEFAULT_REGEX_OPTIONS =
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
        
        private List<Regex> _regexesDomainReview;
        private List<Regex> _regexesDomainWhitelist;
        private List<Regex> _regexesUrlReview;
        private List<Regex> _regexesUrlWhitelist;
        private IEnumerable<Observable> _whitelist;

        public RegexObservablesExtractionUtility(ILogger<RegexObservablesExtractionUtility> logger,
            IObservableWhitelistUtility observableWhitelistUtility,
            IObservableRepository observableRepository)
        {
            _logger = logger;
            _observableWhitelistUtility = observableWhitelistUtility;
            _observableRepository = observableRepository;
        }

        public async Task<HashSet<Observable>> ExtractObservable(string content, DocumentFile documentFile)
        {
            _logger.LogInformation($"Extracting IoC for {documentFile.Filename} ({documentFile.FileId})");

            var observables = new HashSet<Observable>();

            try
            {
                var txt = ReplaceLigatures(content);
                _logger.LogTrace($"Content for {documentFile.Filename} ({documentFile.FileId}) extracted.");
                
                var listDomains = await ExtractDomains(txt);
                _logger.LogTrace($"{listDomains.Count()} domains extracted for {documentFile.Filename} ({documentFile.FileId}).");
                
                var listUrls = await ExtractUrls(txt);
                _logger.LogTrace($"{listUrls.Count()} URL(s) extracted for {documentFile.Filename} ({documentFile.FileId}).");
                
                var listHashes = await ExtractHashes(txt);
                _logger.LogTrace($"{listHashes.Count()} Hash(es) extracted for {documentFile.Filename} ({documentFile.FileId}).");
                
                var listIp = await ExtractIPAddresses(txt);
                _logger.LogTrace($"{listIp.Count()} IP address(es) extracted for {documentFile.Filename} ({documentFile.FileId}).");
                
                observables.UnionWith(listHashes);
                observables.UnionWith(listIp);
                observables.UnionWith(listDomains);
                observables.UnionWith(listUrls);
                _logger.LogTrace($"{observables.Count} indicator(s) of compromised found before deduplication.");

                if (observables.Any())
                {
                    // TODO Use standard 'Distinct' from LINQ for de-duplication
                    observables = observables.GroupBy(u => new
                    {
                        u.Type,
                        Value = u.Type == ObservableType.Artefact || u.Type == ObservableType.File
                            ? u.Hashes.First().Value
                            : u.Value
                    }).Select(u => u.First()).ToHashSet();
                    _logger.LogDebug($"{observables.Count} observables found after deduplication.");

                    await SetHistory(observables);
                    _logger.LogInformation(
                        $"IoC's for {documentFile.Filename} ({documentFile.FileId}) successfully extracted.");
                }
                else
                {
                    _logger.LogInformation($"{documentFile.Filename} ({documentFile.FileId}) contained no observables.");
                }
            }
            catch (Exception e)
            {
                // TODO Do NOT catch Exception. Be More specific.
                _logger.LogWarning(
                    $"Observables from file '{documentFile.Filename}' ({documentFile.FileId}) could not be extracted ({e.Message}).\n{e.StackTrace}");
            }
            return observables;
        }

        /// <summary>
        ///     Populates <c>_regexesDomainWhitelist</c>, <c>_regexesDomainReview</c>, <c>_regexesUrlWhitelist</c>,
        ///     <c>_regexesUrlReview</c> with the corresponding regexes from the database.
        /// </summary>
        private async Task FillWhitelist()
        {
            // Avoid building the list multiple times.
            if (!_whitelist?.Any() ?? true)
            {
                _whitelist = (await _observableWhitelistUtility.GetWhitelistedObservables())?.ToArray();
                if (_whitelist is null)
                {
                    _whitelist = Enumerable.Empty<Observable>();
                    return;   
                }

                _regexesDomainWhitelist = _whitelist
                    .Where(x => x.Type == ObservableType.FQDN && x.Status == ObservableStatus.Whitelisted)
                    .Select(x => new Regex(string.Format(REGEX_FQDN_TEMPLATE, x.Value.Trim()), DEFAULT_REGEX_OPTIONS)).ToList();
                _regexesDomainReview = _whitelist
                    .Where(x => x.Type == ObservableType.FQDN && x.Status == ObservableStatus.Review)
                    .Select(x => new Regex(string.Format(REGEX_FQDN_TEMPLATE, x.Value.Trim()), DEFAULT_REGEX_OPTIONS)).ToList();

                var regexesUrlDomainWhitelist = _whitelist
                    .Where(x => x.Type == ObservableType.FQDN && x.Status == ObservableStatus.Whitelisted)
                    .Select(x => new Regex(string.Format(REGEX_URL_TEMPLATE, x.Value.Trim()), DEFAULT_REGEX_OPTIONS)).ToList();
                var regexesUrlDomainReview = _whitelist
                    .Where(x => x.Type == ObservableType.FQDN && x.Status == ObservableStatus.Review)
                    .Select(x => new Regex(string.Format(REGEX_URL_TEMPLATE, x.Value.Trim()), DEFAULT_REGEX_OPTIONS)).ToList();

                _regexesUrlWhitelist = _whitelist
                    .Where(x => x.Type == ObservableType.URL && x.Status == ObservableStatus.Whitelisted)
                    .Select(x => new Regex(x.Value.Trim(), DEFAULT_REGEX_OPTIONS)).ToList();
                _regexesUrlReview = _whitelist
                    .Where(x => x.Type == ObservableType.URL && x.Status == ObservableStatus.Review)
                    .Select(x => new Regex(x.Value.Trim(), DEFAULT_REGEX_OPTIONS)).ToList();

                _regexesUrlWhitelist.AddRange(regexesUrlDomainWhitelist);
                _regexesUrlReview.AddRange(regexesUrlDomainReview);
            }
        }

        private async Task SetWhitelisted(IEnumerable<Observable> observables, RegexOptions options)
        {
            await FillWhitelist();

            var observablesPreviouslyWhitelisted = observables.Where(observable1 =>
                observable1.Type != ObservableType.URL &&
                observable1.Type != ObservableType.FQDN &&
                (_whitelist?.Any(observable2 =>
                    observable2.ObservableValue() == observable1.ObservableValue() &&
                    observable2.Type == observable1.Type &&
                    observable2.Status == ObservableStatus.Whitelisted) ?? false)).ToList();
            
            foreach(var observable in observablesPreviouslyWhitelisted)
            {
                observable.Status = ObservableStatus.Whitelisted;
                observable.History = ObservableStatus.Whitelisted;
            }

            // TODO Explain why the following code was necessary (remove autoaccepted items???)
            // TODO Why not the FQDNs?
            var observablesPreviouslyReviewed = observables.Where(observable1 =>
                observable1.Type != ObservableType.URL &&
                observable1.Status == ObservableStatus.AutomaticallyAccepted &&
                (_whitelist?.Any(observable2 =>
                    observable2.ObservableValue() == observable1.ObservableValue() &&
                    observable2.Type == observable1.Type &&
                    observable2.Status == ObservableStatus.Review) ?? false)).ToList();
            
            foreach(var observable in observablesPreviouslyReviewed)
            {
                observable.Status = ObservableStatus.Review;
                observable.History = ObservableStatus.Review;
            }

            SetWhitelistedUrl(observables);
            SetWhitelistedDomain(observables);
        }

        private void SetWhitelistedDomain(IEnumerable<Observable> observables)
        {
            foreach (var observable in observables.Where(u => u.Type == ObservableType.FQDN).ToList())
                if (_regexesDomainWhitelist != null)
                    foreach (var regex in _regexesDomainWhitelist.Where(regex => regex.Match(observable.Value).Success))
                    {
                        observable.Status = ObservableStatus.Whitelisted;
                        observable.History = ObservableStatus.Whitelisted;
                        _logger.LogTrace($"Ignore '{observable.Value}' based on '{regex}'.");
                    }

            foreach (var observable in observables.Where(u =>
                u.Type == ObservableType.FQDN && u.Status == ObservableStatus.AutomaticallyAccepted).ToList())
                if (_regexesDomainReview != null)
                    foreach (var regex in _regexesDomainReview.Where(regex => regex.Match(observable.Value).Success))
                    {
                        observable.Status = ObservableStatus.Review;
                        observable.History = ObservableStatus.Review;
                        _logger.LogTrace($"Ignore '{observable.Value}' based on '{regex}'.");
                    }
        }

        private void SetWhitelistedUrl(IEnumerable<Observable> observables)
        {
            foreach (var observable in observables.Where(u => u.Type == ObservableType.URL).ToList())
                if (_regexesUrlWhitelist != null)
                    foreach (var regex in _regexesUrlWhitelist.Where(regex => regex.Match(observable.Value).Success))
                    {
                        observable.Status = ObservableStatus.Whitelisted;
                        observable.History = ObservableStatus.Whitelisted;
                        _logger.LogTrace($"Ignore '{observable.Value}' based on '{regex}'.");
                    }

            foreach (var observable in observables
                .Where(u => u.Type == ObservableType.URL && u.Status == ObservableStatus.AutomaticallyAccepted).ToList())
                if (_regexesUrlReview != null)
                    foreach (var regex in _regexesUrlReview.Where(regex => regex.Match(observable.Value).Success))
                    {
                        observable.Status = ObservableStatus.Review;
                        observable.History = ObservableStatus.Review;
                        _logger.LogTrace($"Ignore '{observable.Value}' based on '{regex}'.");
                    }
        }

        private async Task SetHistory(IEnumerable<Observable> observables)
        {
            // search existing observables in database
            var enumeratedObservables =
                observables.Where(observable => !string.IsNullOrEmpty(observable.Value)).ToArray();
            var matchesEs =
                await _observableRepository.SearchExistingObservables(enumeratedObservables.Where(u =>
                    u.Status != ObservableStatus.Whitelisted));

            //TODO cleanup
            try
            {
                foreach (var observable in enumeratedObservables.Where(p =>
                    matchesEs.Any(u => u.Status == ObservableStatus.Accepted & u.Value == p.Value))) 
                    observable.History = ObservableStatus.Accepted;

                foreach (var observable in enumeratedObservables.Where(p =>
                    matchesEs.Any(u => u.Status == ObservableStatus.Rejected & u.Value == p.Value))) 
                    observable.History = ObservableStatus.Rejected;
            }
            catch (Exception e)
            {
                // TODO Provide a meaningful error message, and do NOT catch Exception.
                // TODO Please be more specific, otherwise, let it go higher in the call stack.
                _logger.LogError($"sethistory ip error ({e.Message}).");
            }

            try
            {
                // REVIEW Check if SequenceEqual is the right method to call, that seems to be a bit restrictive.
                foreach (var i in enumeratedObservables.Where(observable1 =>
                    matchesEs.Where(u => u.Status == ObservableStatus.Accepted && !u.Hashes.IsNullOrEmpty())
                        .Any(observables2 => observables2.Hashes.SequenceEqual(observable1.Hashes))).ToList())
                    i.History = ObservableStatus.Accepted;

                // REVIEW Check if SequenceEqual is the right method to call, that seems to be a bit restrictive.
                foreach (var i in enumeratedObservables.Where(observable1 =>
                    matchesEs.Where(u => u.Status == ObservableStatus.Rejected && !u.Hashes.IsNullOrEmpty())
                        .Any(observables2 => observables2.Hashes.SequenceEqual(observable1.Hashes))).ToList())
                    i.History = ObservableStatus.Rejected;
            }
            catch (Exception e)
            {
                // TODO Provide a meaningful error message, and do NOT catch Exception.
                // TODO Please be more specific, otherwise, let it go higher in the call stack.
                _logger.LogError($"sethistory hash error ({e.Message}).");
            }
        }

        private static string ReplaceLigatures(string txt)
        {
            var sb = new StringBuilder(txt);
            sb.Replace("Ꜳ", "AA");
            sb.Replace("ꜳ", "AA");
            sb.Replace("Æ", "AE");
            sb.Replace("æ", "ae");
            sb.Replace("Ꜵ", "AO");
            sb.Replace("ꜵ", "ao");
            sb.Replace("Ꜷ", "AJ");
            sb.Replace("ꜷ", "aj");
            sb.Replace("Ꜹ", "AV");
            sb.Replace("ꜹ", "av");
            sb.Replace("Ꜻ", "AV");
            sb.Replace("ꜻ", "av");
            sb.Replace("Ꜽ", "AY");
            sb.Replace("ꜽ", "ay");
            sb.Replace("ﬀ", "ff");
            sb.Replace("ﬃ ", "ffi");
            sb.Replace("ﬄ", "ffl");
            sb.Replace("ﬁ", "fi");
            sb.Replace("ﬂ", "fl");
            sb.Replace("Œ", "OE");
            sb.Replace("œ", "oe");
            sb.Replace("Ꝏ", "OO");
            sb.Replace("ꝏ", "oo");
            sb.Replace("ﬆ", "st");
            sb.Replace("ﬅ", "ft");
            sb.Replace("Ꜩ", "TZ");
            sb.Replace("ꜩ", "tz");
            sb.Replace("ᵫ", "ue");
            sb.Replace("Ꝡ", "VY");
            sb.Replace("ꝡ", "vy");
            sb.Replace("…", "...");
            return sb.ToString();
        }

        private async Task<IList<Observable>> ExtractDomains(string text)
        {
            var result = new List<Observable>();

            // Only extract domains that are followed by a whitespace elements
            // REVIEW Can't we add all the punctuation marks? To be tested and further validated wrt false positive rate. 
            var matches = Regex.Matches(text, REGEX_DOMAIN_REGEX + @"(?=[\s\n\r\t\v\f|$])", DEFAULT_REGEX_OPTIONS);
            
            foreach (Match capture in matches)
            {
                // The regex will return the following groups:
                // 0. Complete match
                // 1. First slash
                // 2. The FQDN 
                // 3. The rest of the path

                // Heuristic to skip matches that are likely:
                // - file paths, e.g. /foo/bar/example.org
                // - uri path components, e.g. http://.../some/file.zip
                // - domains inside a uri, e.g. http://example.org/ (handled by URL extraction logic)

                // REVIEW Unclear what this line intends to achieve. Please document the rationale.
                if (!string.IsNullOrEmpty(capture.Groups[1].Value))
                    continue;

                var domain = RefangDots(capture.Groups[2].Value);

                var o = ExtractDomain(domain);
                if (o is not null)
                    result.Add(o);
            }
            // Ignore whitelisted domains & ip's
            await SetWhitelisted(result, DEFAULT_REGEX_OPTIONS);
            return result;
        }

        private Observable ExtractDomain(string domain, bool runRegEx = false)

        {
            var hostTld = domain.Split('.').Last();

            if (runRegEx)
            {
                _logger.LogInformation("Running regex in function");
                var domainMatch = Regex.Match(domain, "^" + REGEX_DOMAIN_REGEX + "$");
                if (!domainMatch.Success)
                    return null;
                _logger.LogInformation($"Regex in function returned true {domainMatch.Captures[0].Value}");
            }

            // Filter out extracted domains that are more likely to generate false positive than true positive.
            if (_TLD.Contains(hostTld.ToUpper()))
            {
                var status = ObservableStatus.AutomaticallyAccepted;
                
                // Manually review suspicious tld's such as common file extensions (e.g. zip, py, pf...)
                if (_TLDToReview.Contains(hostTld.ToUpper()))
                    status = ObservableStatus.Review;
                
                // Manually review if the main hostname part shorter than 3 chars (too expensive)
                if (domain.Split('.')[domain.Split('.').Length - 2].Length < 3)
                    status = ObservableStatus.Review;
                
                // Manually review the domain if there are too many dots (e.g. can be mobile app identifier)
                if (domain.Split('.').Length > 5)
                    status = ObservableStatus.Review;
                
                // Manually review masked IP (e.g 111.111.xxx.xxx)
                var ipRangeMatch = Regex.Match(domain, @"^(?:[0-9xX]{1,3}\.){3}[0-9xX]{1,3}$");
                if (ipRangeMatch.Success)
                    status = ObservableStatus.Review;
                
                // Manually review possible virus detection names
                // TODO Extract possible virus detection names
                var avMatch = Regex.Match(domain,
                    @"^(DOWNLOADER|WIN\.TROJAN|TROJAN|BACKDOOR|WIN32|EXPLOIT|VIRUS|VIRTOOL)\..*$",
                    RegexOptions.IgnoreCase);
                if (avMatch.Success)
                    status = ObservableStatus.Review;
                
                // Manually review if the domain starts with -, probably a false positive
                if (domain.StartsWith('-'))
                    status = ObservableStatus.Review;

                return new Observable
                {
                    Type = ObservableType.FQDN,
                    Value = domain.ToLower(),
                    Status = status,
                    History = status
                };
            }

            return null;
        }

        /// <summary>
        /// Extracts URLs from the specified text. The method also extract domains and IP addresses from the identified
        /// URLs. 
        /// </summary>
        /// <param name="text">A text</param>
        /// <returns>A list of observable found in the text</returns>
        private async Task<IList<Observable>> ExtractUrls(string text)
        {
            var result = new List<Observable>();
            var uris = new HashSet<Uri>();
            
            var matches = Regex.Matches(text, GENERIC_URL_REGEX, DEFAULT_REGEX_OPTIONS);
            foreach (Match capture in matches)
            {
                var url = NormalizeUrl(capture.Groups[1].Value);
                _logger.LogTrace($"Extracted {url} with GENERIC_URL_REGEX ({capture.ToString()}).");
                if (url is not null) uris.Add(url);
            }

            matches = Regex.Matches(text, BRACKET_URL_REGEX, DEFAULT_REGEX_OPTIONS);
            foreach (Match capture in matches)
            {
                var url = NormalizeUrl(capture.Groups[1].Value);
                _logger.LogTrace($"Extracted {url} with BRACKET_URL_REGEX ({capture.ToString()}).");
                if (url is not null) uris.Add(url);
            }

            _logger.LogTrace($"Extracted {uris.Count} urls.");

            // REVIEW Why was it commented? Lack of testing or too high false positive rate. If too high positive rate, please delete the code (and related code as well)
            // matches = Regex.Matches(txt, BACKSLASH_URL_RE, options);
            // foreach (Match capture in matches)
            // {
            //     URLs.Add(NormalizeURL(capture.Groups[1].Value));
            // }
            // _logger.LogDebug($"Extracted {URLs.Count} urls (BACKSLASH_URL_RE).");

            _logger.LogDebug($"Extracted {uris.Count} urls.");
            foreach (var uri in uris)
            {
                // REVIEW Are we sure that it is the best option, this condition will prevent extraction of URLs without a path (i.e. http://www.badstuff.com/ is not extracted as a URL)
                if (string.IsNullOrEmpty(uri.PathAndQuery) || uri.PathAndQuery == "/" || uri.PathAndQuery == "/*")
                {
                    var isIPv4URI = Regex.Match(uri.Host, IPV4_REGEX, DEFAULT_REGEX_OPTIONS);
                    if (isIPv4URI.Success)
                    {
                        var observable = ExtractIPAddress(RefangIPv4(isIPv4URI.Value));
                        if (observable is not null)
                            result.Add(observable);
                    }
                    else
                    {
                        var observable = ExtractDomain(uri.Host, true);
                        if (observable is not null)
                            result.Add(observable);
                    }
                }
                else
                {
                    result.Add(new Observable
                    {
                        Type = ObservableType.URL, 
                        Value = uri.ToString(),
                        Status = ObservableStatus.AutomaticallyAccepted, 
                        History = ObservableStatus.AutomaticallyAccepted
                    });
                }
            }

            await SetWhitelisted(result, DEFAULT_REGEX_OPTIONS);

            return result;
        }

        private static string RefangDots(string url)
        {
            var sb = new StringBuilder(url);
            RefangDots(sb);
            return sb.ToString();
        }

        private static void RefangDots(StringBuilder sb)
        {
            sb.Replace("[.]", ".");
            sb.Replace("(.)", ".");
            sb.Replace(" DOT ", ".");
            sb.Replace("[DOT]", ".");
            sb.Replace("[dot]", ".");
            sb.Replace("(DOT)", ".");
            sb.Replace("(dot)", ".");
        }

        private Uri NormalizeUrl(string url)
        {
            _logger.LogDebug("Normalize: " + url);
            
            // TODO Use StringBuilder in the function, and not string. As StringBuilder are for mutable strings and likely more efficient in this context.
            var mutableURL = new StringBuilder(url);
            if (url.Contains("[.") & !url.Contains("[.]")) mutableURL.Replace("[.", "[.]");
            if (url.Contains(".]") & !url.Contains("[.]")) mutableURL.Replace(".]", "[.]");
            if (url.Contains("[dot") & !url.Contains("[dot]")) mutableURL.Replace("[dot", "[.]");
            if (url.Contains("dot]") & !url.Contains("[dot]")) mutableURL.Replace("dot]", "[.]");
            if (url.Contains("[/]")) mutableURL.Replace("[/]", "/");
            url = mutableURL.ToString();
                
            // Ensure a scheme exists
            if (!url.Contains("//"))
            {
                // Get the 8 first character of the url, that should contain the scheme if it exists and attempt
                // to fix the URL properly, refanging what is needed. 
                var url8 = url.Length >= 8 ? url.Substring(0, 8) : url;
                if (url8.Contains("__"))
                {
                    // Refang http__domain and http:__domain.
                    if (url8.Contains(":__"))
                        url = ReplaceFirst(url, ":__", "://");
                    else
                        url = ReplaceFirst(url, "__", "://");
                }
                else if (url8.Contains("\\\\"))
                {
                    // Refang http:\\domain
                    url = ReplaceFirst(url, "\\\\", "//");
                }
                else
                {
                    // Refang no-protocol
                    url = "http://" + url;
                }
            }

            // Refang (/) and  some backslash-escaped characters.
            mutableURL = new StringBuilder(url);
            mutableURL.Replace("(/)", "/");
            mutableURL.Replace(@"\.", ".");
            mutableURL.Replace(@"\(", "(");
            mutableURL.Replace(@"\[", "[");
            mutableURL.Replace(@"\)", ")");
            mutableURL.Replace(@"\]", "]");

            // Refang dots, or Uri won't parse.
            RefangDots(mutableURL);

            // TODO Avoid enclosing a try/catch block in other, both catching Exception. Use more specific Exception. 
            try
            {
                var uri = new Uri(url);

                try
                {
                    var uriBuilder = new UriBuilder(uri);

                    // Handle URLs with no scheme / obfuscated scheme.
                    if (!new[] {"http", "https", "ftp"}.Contains(uri.Scheme))
                    {
                        if (new[] {"ftx", "fxp"}.Contains(uri.Scheme.TrimEnd('s')))
                            uriBuilder.Scheme = "ftp";
                        else if (new[] {"hxxp"}.Contains(uri.Scheme.TrimEnd('s')))
                            uriBuilder.Scheme = "https";
                        else
                            uriBuilder.Scheme = "http";
                    }

                    // Remove artifacts from common defangs.
                    uriBuilder.Host = NormalizeCommon(uriBuilder.Host);
                    uriBuilder.Path = RefangDots(uriBuilder.Path);

                    // Fix example[.]com, but keep IPv6 URLs (see RFC 2732 for details) intact.
                    // IPv6 URLs look like http://[::FFFF:129.144.52.38]:80/index.html
                    if (!IsIPv6Url(uriBuilder.Uri))
                        uriBuilder.Host = uriBuilder.Host.Replace("[", "").Replace("]", "");
                    
                    return uriBuilder.Uri;
                }
                catch (Exception e)
                {
                    // TODO Provide a meaningful error message, and do NOT catch Exception.
                    // TODO Please be more specific, otherwise, let it go higher in the call stack.
                    _logger.LogError($"Error urlbuilder {url} " + e.Message);
                }
            }
            catch (Exception e)
            {
                // TODO Provide a meaningful error message, and do NOT catch Exception.
                // TODO Please be more specific, otherwise, let it go higher in the call stack.
                _logger.LogWarning($"Could not convert to uri : {url} " + e.Message);
                // Last resort on ipv6 fail.
                // uri = new Uri(url.Replace("[", "").Replace("]", ""));
            }

            return null;
        }

        private async Task<IEnumerable<Observable>> ExtractHashes(string text)
        {
            var md5Hashes = ExtractHash(text, MD5_REGEX, ObservableHashType.MD5).ToArray();
            _logger.LogDebug($"Extracted {md5Hashes.Count()} MD5 hashes.");
            
            var sha1Hashes = ExtractHash(text, SHA1_REGEX, ObservableHashType.SHA1).ToArray();
            _logger.LogDebug($"Extracted {sha1Hashes.Count()} SHA1 hashes.");
            
            var sha256Hashes = ExtractHash(text, SHA256_REGEX, ObservableHashType.SHA256).ToArray();
            _logger.LogDebug($"Extracted {sha256Hashes.Count()} SHA256 hashes.");
            
            var sha512Hashes = ExtractHash(text, SHA512_REGEX, ObservableHashType.SHA512).ToArray();
            _logger.LogDebug($"Extracted {sha512Hashes.Count()} SHA512 hashes.");

            var observables = md5Hashes.Union(sha1Hashes).Union(sha256Hashes).Union(sha512Hashes);
            await SetWhitelisted(observables, DEFAULT_REGEX_OPTIONS);
            return observables;
        }

        private static IEnumerable<Observable> ExtractHash(string text, string pattern, ObservableHashType hashType)
        {
            var matches = Regex.Matches(text, pattern, DEFAULT_REGEX_OPTIONS);
            return matches.Distinct()
                .Select(capture => capture.Groups[1].Value)
                .Select(extractedHash => new Observable
                {
                    Type = ObservableType.File,
                    Hashes = new List<ObservableHash>
                    {
                        new()
                        {
                            Value = extractedHash,
                            HashType = hashType
                        }
                    },
                    Status = ObservableStatus.AutomaticallyAccepted,
                    History = ObservableStatus.AutomaticallyAccepted
                }); 
        }

        private Observable ExtractIPAddress(string extractedIP)
        {
            // TODO This should probably be optional, there might be use case where you want to have them extracted.
            // TODO Implement this properly, i.e. not with an inefficient regex.
            // I would rather have them included but automatically whitelisted.
            var privateIPRegex =
                @"(^0\.)|(^127\.)|(^10\.)|(^172\.1[6-9]\.)|(^172\.2[0-9]\.)|(^172\.3[0-1]\.)|(^192\.168\.)";
            var m = Regex.Match(extractedIP, privateIPRegex, DEFAULT_REGEX_OPTIONS);
            if (m.Success)
            {
                _logger.LogTrace($"IP Address '{extractedIP}' excluded from indicators (local IP address).");
                return null;
            }

            var observable = new Observable
            {
                Type = ObservableType.IPv4, 
                Value = extractedIP,
                Status = ObservableStatus.AutomaticallyAccepted, 
                History = ObservableStatus.Review
            };
            _logger.LogTrace($"IP Address '{extractedIP}' extracted as indicator.");
            return observable;
        }

        private async Task<IEnumerable<Observable>> ExtractIPAddresses(string txt)
        {
            var options = DEFAULT_REGEX_OPTIONS | RegexOptions.Compiled;
            var matches = Regex.Matches(txt, IPV4_REGEX, options);
            var observables = matches.Select(capture =>
            {
                var extractedIp = RefangIPv4(capture.Groups[0].Value);
                return ExtractIPAddress(extractedIp);
            }).Where(observable => observable != null);
            _logger.LogDebug($"Extracted {observables.Count()} IPv4 addresses.");

            // TODO Check if CIDR ranges are properly extracted, otherwise add proper extraction.

            await SetWhitelisted(observables, options);
            return observables;
        }
        
        private string RefangIPv4(string ip)
        {
            var mutableIPAddress = new StringBuilder(ip);
            NormalizeCommon(mutableIPAddress);
            mutableIPAddress.Replace("[", "");
            mutableIPAddress.Replace("]", "");
            mutableIPAddress.Replace(@"\\", "");
            return mutableIPAddress.ToString();
        }
        
        private static string ReplaceFirst(string text, string search, string replace)
        {
            var pos = text.IndexOf(search);
            if (pos < 0) return text;
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private static string NormalizeCommon(string ioc)
        {
            var sb = new StringBuilder(ioc);
            NormalizeCommon(sb);
            return sb.ToString();
        }

        private static void NormalizeCommon(StringBuilder sb)
        {
            sb.Replace("[dot]", ".");
            sb.Replace("(dot)", ".");
            sb.Replace("[.]", ".");
            sb.Replace("(", "");
            sb.Replace(")", "");
            sb.Replace(",", ".");
            sb.Replace(" ", "");
            sb.Replace("\u30fb", ".");
        }

        private bool IsIPv6Url(Uri parsed)
        {
            string ipv6;
            // Handle RFC 2732 IPv6 URLs with and without port, as well as non-RFC IPv6 URLs.
            if (parsed.Host.Contains("]:"))
                ipv6 = string.Join(':', parsed.Host.Split(':').Reverse().Skip(1).Reverse());
            else
                ipv6 = parsed.Host;

            if (IPAddress.TryParse(ipv6, out var address))
                switch (address.AddressFamily)
                {
                    case AddressFamily.InterNetworkV6:
                        return true;
                    default:
                        return false;
                }

            return false;
        }
        
        // TODO Implement more observables from the text.
#if false
        private static string _bitcoinAddressRegex = @"\s([13][a-km-zA-HJ-NP-Z0-9]{26,33})";
        private static string _moneraAddressRegex = @"\s(4([0-9]|[A-B])(.){93})";

        private static string _hklmRegistryKeyRegex =
            @"((HKEY_LOCAL_MACHINE\\|HKLM\\)((([a-zA-Z0-9_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+)|([a-zA-Z0-9\s_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+([\\]+)))";

        private static string _hkccRegistryKeyRegex =
            @"((HKEY_CURRENT_CONFIG\\|HKCC\\)((([a-zA-Z0-9_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+)|([a-zA-Z0-9\s_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+([\\]+)))";

        private static string _hkcrRegistryKeyRegex =
            @"((HKEY_CLASSES_ROOT\\|HKCR\\)((([a-zA-Z0-9_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+)|([a-zA-Z0-9\s_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+([\\]+)))";

        private static string _hkcuRegistryKeyRegex =
            @"((HKEY_CURRENT_USER\\|HKCU\\)((([a-zA-Z0-9_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+)|([a-zA-Z0-9\s_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+([\\]+)))";

        private static string _hkuRegistryKeyRegex =
            @"((HKEY_USERS\\|HKU\\)((([a-zA-Z0-9_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+)|([a-zA-Z0-9\s_@\-\^!#\.\:\/\$%&+={}\[\]\\*])+([\\]+)))";

        private void ExtractCryptoCurrencyAddress(Guid fileId, string txt)
        {
            var options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;

            if (Regex.Match(txt, @"[^\w]+(btc|bitcoin)", options).Success) {
                var cache = new HashSet<string>();
                var matches = Regex.Matches(txt, BITCOIN_ADDR_RE, options);
                foreach (Match capture in matches)
                {
                    cache.Add(NormalizeCryptoCurrencyAddress(capture.Groups[1].Value));
                }

                _logger.LogInformation($"Extracted {cache.Count} BitCoin addresses.");
                foreach (var addr in cache) {
                    _context.Add(new Observable {
                        DocumentId = documentId,
                        Type = "btc",
                        Value = addr
                    });
                }
            }

            if (Regex.Match(txt, @"[^\w]+(xmr|monero)", options).Success) {
                var cache = new HashSet<string>();
                var matches = Regex.Matches(txt, MONERO_ADDR_RE, options);
                foreach (Match capture in matches)
                {
                    cache.Add(NormalizeCryptoCurrencyAddress(capture.Groups[1].Value));
                }

                _logger.LogInformation($"Extracted {cache.Count} Monero addresses.");
                foreach (var addr in cache) {
                    _context.Add(new Observable {
                        DocumentId = documentId,
                        Type = "xmr",
                        Value = addr
                    });
                }
            }
        }

        private void ExtractCVE(Guid fileId, string txt)
        {
            var options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
            var cache = new HashSet<string>();

            var matches = Regex.Matches(txt, CVE_RE, options);
            foreach (Match capture in matches)
            {
                cache.Add(NormalizeCVE(capture.Value));
            }

            _logger.LogInformation($"Extracted {cache.Count} CVE.");
            foreach (var addr in cache) {
                _context.Add(new Observable {
                    DocumentId = documentId,
                    Type = "cve",
                    Value = addr
                });   
            }
        }

        private void ExtractMitreAttack(Guid fileId, string txt)
        {
            var options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
            var cache = new HashSet<string>();

            var matches = Regex.Matches(txt, MITRE_ATTACK_ID_RE, options);
            foreach (Match capture in matches)
            {
                cache.Add(capture.Groups[1].Value.ToUpper());
            }

            _logger.LogInformation($"Extracted {cache.Count} Mitre Att&ck identifiers.");
            foreach (var id in cache) {
                _context.Add(new Observable {
                    DocumentId = documentId,
                    Type = "mitre-attack",
                    Value = id
                });
            }
        }

        private void ExtractRegisteryKey(Guid fileId, string txt)
        {
            var options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;

            var cache = new HashSet<string>();
            var matches = Regex.Matches(txt, HKLM, options);
            foreach (Match capture in matches)
            {
                var regkey = capture.Groups[1].Value;
                _logger.LogDebug(capture.Value.ToString());
                regkey = regkey.Replace("HKLM", "HKEY_LOCAL_MACHINE");
                cache.Add(regkey);
            }

            _logger.LogInformation($"Extracted {cache.Count} HKLM registery keys.");
            foreach (var addr in cache) {
                _context.Add(new Observable {
                    DocumentId = documentId,
                    Type = "regkey",
                    Value = addr
                });
            }

            cache = new HashSet<string>();
            matches = Regex.Matches(txt, HKCR, options);
            foreach (Match capture in matches)
            {
                var regkey = capture.Groups[1].Value;
                regkey = regkey.Replace("HKCR", "HKEY_CLASSES_ROOT");
                cache.Add(regkey);
            }

            _logger.LogInformation($"Extracted {cache.Count} HKCR registery keys.");
            foreach (var addr in cache) {
                _context.Add(new Observable {
                    DocumentId = documentId,
                    Type = "regkey",
                    Value = addr
                });
            }

            cache = new HashSet<string>();
            matches = Regex.Matches(txt, HKCU, options);
            foreach (Match capture in matches)
            {
                var regkey = capture.Groups[1].Value;
                regkey = regkey.Replace("HKCU", "HKEY_CURRENT_USER");
                cache.Add(regkey);
            }

            _logger.LogInformation($"Extracted {cache.Count} HKCU registery keys.");
            foreach (var addr in cache) {
                _context.Add(new Observable {
                    DocumentId = documentId,
                    Type = "regkey",
                    Value = addr
                });
            }

            cache = new HashSet<string>();
            matches = Regex.Matches(txt, HKU, options);
            foreach (Match capture in matches)
            {
                var regkey = capture.Groups[1].Value;
                regkey = regkey.Replace("HKU", "HKEY_USERS");
                cache.Add(regkey);
            }

            _logger.LogInformation($"Extracted {cache.Count} HKU registery keys.");
            foreach (var addr in cache) {
                _context.Add(new Observable {
                    DocumentId = documentId,
                    Type = "regkey",
                    Value = addr
                });
            }

            cache = new HashSet<string>();
            matches = Regex.Matches(txt, HKCC, options);
            foreach (Match capture in matches)
            {
                var regkey = capture.Groups[1].Value;
                regkey = regkey.Replace("HKCC", "HKEY_CURRENT_CONFIG");
                cache.Add(regkey);
            }

            _logger.LogInformation($"Extracted {cache.Count} HKCC registery keys.");
            foreach (var addr in cache) {
                _context.Add(new Observable {
                    DocumentId = documentId,
                    Type = "regkey",
                    Value = addr
                });
            }
        }
        
        private string NormalizeCve(string addr)
        {
            return addr.ToUpper();
        }

        private string NormalizeCryptoCurrencyAddress(string addr)
        {
            return addr.ToLower();
        }

        private void ExtractEmails(Guid fileId, string txt)
        {
            var options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
            var cache = new HashSet<string>();

            var matches = Regex.Matches(txt, EMAIL_RE, options);
            foreach (Match capture in matches)
            {
                cache.Add(NormalizeEmail(capture.Groups[1].Value));
            }

            // Ignore whitelisted domains
            var regexes = _context.WhiteListObservables.Where(x => x.Type == "domain").Select(x => new Regex(x.Value.Trim(), options));
            foreach (var email in cache.ToList()) {
                var domainEmail = email.Substring(email.LastIndexOf('@') + 1);
                foreach (var regex in regexes) {
                    if(regex.Match(domainEmail).Success) {
                        cache.Remove(email);
                    }
                }
            }
            // Ignore whitelisted emails
            regexes = _context.WhiteListObservables.Where(x => x.Type == "email").Select(x => new Regex(x.Value.Trim(), options));
            foreach (var hash in cache.ToList()) {
                foreach (var regex in regexes) {
                    if(regex.Match(hash).Success) {
                        cache.Remove(hash);
                    }
                }
            }
            foreach (var c in cache) {
                _context.Add(new Observable {
                    DocumentId = documentId,
                    Type = "email",
                    Value = c
                });
            }
            _logger.LogInformation($"Extracted {cache.Count} Email addresses.");
        }

        private string NormalizeEmail(string email)
        {
            email = Regex.Replace(email.ToLower(), @"\W[aA][tT]\W", "@");
            email = Regex.Replace(email, @"\W*[dD][oO][tT]\W*", ".");
            return NormalizeCommon(email).Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "");
        }

        private static string _cveRegex = @"CVE-\d{4}-\d{4,7}";
        private static string _mitreAttackRegex = @"\s(T1\d{3})";
        private static string _urlSplitStr = @"[>""'\),};]";

        // Suspiciously unused field, to review.
        // ReSharper disable once UnusedMember.Local
        public const string EMAIL_REGEX = @"
            (
                [a-z0-9_.+-]+
                [\(\[{\x20]*
                (?:@|\Wat\W)
                [\)\]}\x20]*
                [a-z0-9-]+
                (?:
                    (?:
                        (?:
                            \x20*
                            " + SEPARATOR_DEFANGS + @"
                            \x20*
                        )*
                        \.
                        (?:
                            \x20*
                            " + SEPARATOR_DEFANGS + @"
                            \x20*
                        )*
                        |
                        \W+dot\W+
                    )
                    [a-z0-9-]+?
                )+
            )
        " + END_PUNCTUATION + @"
            (?=\s|$)
        ";

#endif
    }
}