using GalaxyCheck.Gens;
using System.Collections.Generic;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>
        /// Generators of realistic English data. Each value comes from fixed lists in the library, and a failing
        /// value shrinks towards the first entry of each list. The lists hold no real private data: the names are
        /// common given names and surnames, the companies are fictional, and the phone numbers are in the 555-01xx
        /// range, which is reserved for fiction.
        /// </summary>
        [global::Verifiable.Contracts.Unverifiable]
        public static class Realistic
        {
            /// <summary>The list of first names.</summary>
            public static IReadOnlyList<string> FirstNames => RealisticData.FirstNames;

            /// <summary>The list of last names.</summary>
            public static IReadOnlyList<string> LastNames => RealisticData.LastNames;

            /// <summary>Generates a first name of <see cref="FirstNames"/>.</summary>
            public static IGen<string> FirstName() => Element(RealisticData.FirstNames);

            /// <summary>Generates a last name of <see cref="LastNames"/>.</summary>
            public static IGen<string> LastName() => Element(RealisticData.LastNames);

            /// <summary>Generates a first name, a space, and a last name.</summary>
            public static IGen<string> FullName() =>
                from first in FirstName()
                from last in LastName()
                select first + " " + last;

            /// <summary>Generates a street address, for example <c>12 Oak Street</c>.</summary>
            public static IGen<string> StreetAddress() =>
                from number in Int32().Between(1, 9999)
                from name in Element(RealisticData.StreetNames)
                from suffix in Element(RealisticData.StreetSuffixes)
                select number.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + name + " " + suffix;

            /// <summary>Generates the name of a well-known city.</summary>
            public static IGen<string> City() => Element(RealisticData.Cities);

            /// <summary>Generates the name of a country.</summary>
            public static IGen<string> Country() => Element(RealisticData.Countries);

            /// <summary>
            /// Generates a phone number in the format <c>NNN-555-01NN</c>. The 555-01xx range is reserved for fiction.
            /// </summary>
            public static IGen<string> PhoneNumber() => StringMatching(@"[2-9][0-9]{2}-555-01[0-9]{2}");

            /// <summary>Generates the name of a fictional company, for example <c>Acme Corporation</c>.</summary>
            public static IGen<string> CompanyName() => Element(RealisticData.CompanyNames);
        }
    }
}

namespace GalaxyCheck.Gens
{
    /// <summary>The fixed English lists of the realistic generators.</summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal static class RealisticData
    {
        public static readonly string[] FirstNames =
        {
            "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda", "William", "Elizabeth",
            "David", "Barbara", "Richard", "Susan", "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen",
            "Christopher", "Nancy", "Daniel", "Lisa", "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra",
            "Donald", "Ashley", "Steven", "Kimberly", "Paul", "Emily", "Andrew", "Donna", "Joshua", "Michelle",
            "Kenneth", "Dorothy", "Kevin", "Carol", "Brian", "Amanda", "George", "Melissa", "Edward", "Deborah",
            "Ronald", "Stephanie", "Timothy", "Rebecca", "Jason", "Sharon", "Jeffrey", "Laura", "Ryan", "Cynthia",
            "Jacob", "Kathleen", "Gary", "Amy", "Nicholas", "Shirley", "Eric", "Angela", "Jonathan", "Helen",
            "Stephen", "Anna", "Larry", "Brenda", "Justin", "Pamela", "Scott", "Nicole", "Brandon", "Emma",
            "Benjamin", "Samantha", "Samuel", "Katherine", "Gregory", "Christine", "Frank", "Debra", "Alexander", "Rachel",
            "Raymond", "Catherine", "Patrick", "Carolyn", "Jack", "Janet", "Dennis", "Ruth", "Jerry", "Maria",
            "Oliver", "Olivia", "Henry", "Sophia", "Lucas", "Isabella", "Noah", "Ava", "Liam", "Mia",
            "Ethan", "Charlotte", "Mason", "Amelia", "Logan", "Harper", "Aiden", "Evelyn", "Owen", "Grace",
        };

        public static readonly string[] LastNames =
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Wilson", "Anderson", "Taylor",
            "Thomas", "Moore", "Jackson", "Martin", "Lee", "Thompson", "White", "Harris", "Clark", "Lewis",
            "Robinson", "Walker", "Young", "Allen", "King", "Wright", "Scott", "Hill", "Green", "Adams",
            "Baker", "Nelson", "Carter", "Mitchell", "Roberts", "Turner", "Phillips", "Campbell", "Parker", "Evans",
            "Edwards", "Collins", "Stewart", "Morris", "Murphy", "Cook", "Rogers", "Morgan", "Cooper", "Peterson",
            "Bailey", "Reed", "Kelly", "Howard", "Cox", "Ward", "Richardson", "Watson", "Brooks", "Wood",
            "James", "Bennett", "Gray", "Hughes", "Price", "Sanders", "Myers", "Long", "Ross", "Foster",
            "Powell", "Jenkins", "Perry", "Russell", "Sullivan", "Bell", "Coleman", "Butler", "Henderson", "Barnes",
            "Fisher", "Graham", "Hunt", "Palmer", "Mason", "Stone", "Webb", "Ellis", "Shaw", "Holmes",
            "Fox", "Lane", "Hart", "Dixon", "Owens", "Harper", "Marshall", "Wells", "Fletcher", "Burke",
        };

        public static readonly string[] StreetNames =
        {
            "Main", "Oak", "Pine", "Maple", "Cedar", "Elm", "Park", "Lake", "Hill", "Church",
            "Mill", "River", "Spring", "Forest", "Meadow", "Sunset", "Highland", "Willow", "Station", "Bridge",
            "North", "South", "East", "West", "High", "Market", "Garden", "Orchard", "Valley", "Ridge",
            "Chestnut", "Birch", "Walnut", "Poplar", "Ash", "Holly", "Rose", "Cherry", "Harbor", "Lincoln",
            "Washington", "Franklin", "Jefferson", "Madison", "Victoria", "Queen", "King", "Kings", "Green", "Long",
        };

        public static readonly string[] StreetSuffixes =
        {
            "Street", "Avenue", "Road", "Lane", "Drive", "Court", "Place", "Way", "Boulevard", "Terrace",
        };

        public static readonly string[] Cities =
        {
            "London", "New York", "Paris", "Tokyo", "Sydney", "Toronto", "Berlin", "Madrid", "Rome", "Dublin",
            "Edinburgh", "Manchester", "Chicago", "Boston", "Seattle", "San Francisco", "Los Angeles", "Vancouver", "Melbourne", "Auckland",
            "Wellington", "Amsterdam", "Brussels", "Vienna", "Prague", "Budapest", "Warsaw", "Stockholm", "Oslo", "Copenhagen",
            "Helsinki", "Lisbon", "Athens", "Istanbul", "Cairo", "Nairobi", "Cape Town", "Mumbai", "Delhi", "Singapore",
            "Hong Kong", "Seoul", "Beijing", "Shanghai", "Bangkok", "Mexico City", "Buenos Aires", "Lima", "Santiago", "Rio de Janeiro",
        };

        public static readonly string[] Countries =
        {
            "United Kingdom", "United States", "Canada", "Australia", "New Zealand", "Ireland", "France", "Germany", "Spain", "Italy",
            "Portugal", "Netherlands", "Belgium", "Austria", "Switzerland", "Sweden", "Norway", "Denmark", "Finland", "Iceland",
            "Poland", "Czechia", "Hungary", "Greece", "Turkey", "Egypt", "Kenya", "South Africa", "Nigeria", "Morocco",
            "India", "China", "Japan", "South Korea", "Singapore", "Thailand", "Vietnam", "Indonesia", "Malaysia", "Philippines",
            "Mexico", "Brazil", "Argentina", "Chile", "Peru", "Colombia", "Uruguay", "Jamaica", "Cuba", "Fiji",
        };

        // Companies from well-known fiction, so that no name belongs to a real company.
        public static readonly string[] CompanyNames =
        {
            "Acme Corporation", "Globex Corporation", "Initech", "Hooli", "Vandelay Industries", "Wonka Industries", "Soylent Corporation", "Cyberdyne Systems", "Tyrell Corporation", "Wayne Enterprises",
            "Oscorp", "Massive Dynamic", "Dunder Mifflin", "Prestige Worldwide", "Sterling Cooper", "Pied Piper", "Bluth Company", "Planet Express", "Spacely Sprockets", "Cogswell Cogs",
            "Virtucon", "Wernham Hogg", "Monsters Incorporated", "Stark Industries", "Umbrella Corporation", "Weyland-Yutani", "Aperture Science", "Black Mesa", "Gringotts", "Krusty Krab",
        };
    }
}
