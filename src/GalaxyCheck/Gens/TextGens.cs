namespace GalaxyCheck
{
    public static partial class Gen
    {
        private const string UriPattern =
            @"https?://[a-z][a-z0-9]{0,9}(\.[a-z][a-z0-9]{0,9}){0,2}\.(com|org|net|io|dev|example)(/[a-z0-9]{1,8}){0,3}";

        private const string EmailPattern =
            @"[a-z][a-z0-9]{0,11}([._][a-z0-9]{1,8})?@[a-z][a-z0-9]{0,9}\.(com|org|net|io|dev|example)";

        /// <summary>
        /// Generates absolute http and https URIs, with a host and up to three path segments. A value shrinks towards
        /// <c>http://a.com</c>.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IGen<System.Uri> Uri() =>
            StringMatching(UriPattern).Select(text => new System.Uri(text, System.UriKind.Absolute));

        /// <summary>
        /// Generates <see cref="System.Version"/> values with 2 to 4 parts. A value shrinks towards <c>0.0</c>.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IGen<System.Version> Version()
        {
            var part = Int32().Between(0, 1000);
            return
                from partCount in Int32().Between(2, 4)
                from major in part
                from minor in part
                from build in partCount >= 3 ? part : Constant(0)
                from revision in partCount == 4 ? part : Constant(0)
                select partCount switch
                {
                    2 => new System.Version(major, minor),
                    3 => new System.Version(major, minor, build),
                    _ => new System.Version(major, minor, build, revision)
                };
        }

        /// <summary>
        /// Generates email addresses with one <c>@</c>, a local part, and a domain with a dot.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IGen<string> Email() => StringMatching(EmailPattern);
    }
}
