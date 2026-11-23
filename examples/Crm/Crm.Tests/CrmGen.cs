using GalaxyCheck;

namespace Crm.Tests;

/// <summary>Realistic generators of CRM data.</summary>
public static class CrmGen
{
    public static IGen<DateOnly> Day => Gen.DateOnly().Within(new DateOnly(2020, 1, 1), new DateOnly(2026, 12, 31));

    /// <summary>North American numbers in the usual ways to write them, for example "(212) 555-0175" or "212.555.0175".</summary>
    public static IGen<string> NorthAmericanPhone => Gen.StringMatching(@"(1-)?\(?[2-9][0-9]{2}\)?[ .-]?[2-9][0-9]{2}[ .-]?[0-9]{4}");

    /// <summary>International numbers for the UK, Germany, Australia, and New Zealand, with "+" or "00".</summary>
    public static IGen<string> InternationalPhone => Gen.StringMatching(@"(\+|00)(44|49|61|64) ?[1-9][0-9]{2} ?[0-9]{3} ?[0-9]{3}");

    public static IGen<string> Phone => Gen.Choose(
        (Gen.Realistic.PhoneNumber(), 2),
        (NorthAmericanPhone, 2),
        (InternationalPhone, 1));

    public static IReadOnlyList<string> TagNames { get; } = ["vip", "newsletter", "partner", "churn-risk", "trial"];

    /// <summary>A tag as a person types it: in any case, sometimes with spaces.</summary>
    public static IGen<string> Tag =>
        from tag in Gen.Element(TagNames)
        from shout in Gen.Boolean()
        from pad in Gen.Boolean()
        let text = shout ? tag.ToUpperInvariant() : tag
        select pad ? " " + text + " " : text;

    /// <summary>
    /// Contacts from automatic generation. Gen.Create builds each member from its type, and each override gives a
    /// member realistic values: a random string is a poor first name and a worse email address.
    /// </summary>
    public static IGen<Contact> Contact => ContactWith(Gen.Email(), Phone);

    public static IGen<Contact> ContactWith(IGen<string> email, IGen<string> phone) =>
        Gen.Create<Contact>()
            .OverrideMember(c => c.FirstName, Gen.Realistic.FirstName())
            .OverrideMember(c => c.LastName, Gen.Realistic.LastName())
            .OverrideMember(c => c.Emails, email.ListOf().WithCountLessThanEqual(2))
            .OverrideMember(c => c.Phones, phone.ListOf().WithCountLessThanEqual(2))
            .OverrideMember(c => c.Tags, Tag.ListOf().WithCountLessThanEqual(3))
            .OverrideMember(c => c.CreatedOn, Day)
            .OverrideMember(c => c.LastActivityOn, Day);

    /// <summary>
    /// Contacts whose addresses and numbers come from small shared pools, so that many contacts are duplicates. An
    /// address sometimes comes in upper case or with a sub-address, as people type it.
    /// </summary>
    public static IGen<IReadOnlyList<Contact>> ContactsWithDuplicates =>
        from emails in Gen.Email().ListOf().WithCount(4)
        from phones in NorthAmericanPhone.ListOf().WithCount(4)
        from contacts in UniqueIds(ContactWith(Spelling(Gen.Element(emails)), Gen.Element(phones)).ListOf().WithCountLessThanEqual(12))
        select contacts;

    /// <summary>Each contact of a list has its own ID, as in a real CRM. Without this filter, shrinking makes the IDs
    /// equal, because it moves each ID toward the smallest Guid.</summary>
    public static IGen<IReadOnlyList<Contact>> UniqueIds(IGen<IReadOnlyList<Contact>> contacts) =>
        contacts.Where(list => list.Select(c => c.Id).Distinct().Count() == list.Count);

    private static IGen<string> Spelling(IGen<string> email) =>
        from address in email
        from variant in Gen.Int32().Between(0, 2)
        select variant switch
        {
            0 => address,
            1 => address.ToUpperInvariant(),
            _ => address.Replace("@", "+crm@"),
        };

    /// <summary>Company groups up to three levels deep, with up to three subsidiaries for each company.</summary>
    public static IGen<Company> CompanyGroup => Gen.Recursive(
        from name in Gen.Realistic.CompanyName()
        from employees in Gen.Int32().Between(0, 300)
        select Company.Single(name, employees),
        inner =>
            from name in Gen.Realistic.CompanyName()
            from employees in Gen.Int32().Between(0, 300)
            from subsidiaries in inner.ListOf().WithCountBetween(1, 3)
            select new Company(name, employees, subsidiaries),
        maxDepth: 3);

    public static IGen<Activity> ActivityOn(IGen<DateOnly> day) =>
        from date in day
        from kind in Gen.Element(new[] { "email", "call", "meeting", "web visit" })
        select new Activity(date, kind);
}
