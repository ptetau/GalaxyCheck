namespace Crm;

/// <summary>A person in the CRM. A contact can have more than one email address and phone number.</summary>
public sealed record Contact
{
    public required Guid Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public IReadOnlyList<string> Emails { get; init; } = [];

    public IReadOnlyList<string> Phones { get; init; } = [];

    public IReadOnlyList<string> Tags { get; init; } = [];

    public DateOnly CreatedOn { get; init; }

    public DateOnly LastActivityOn { get; init; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>The normalised addresses. An address that does not normalise is not a key for deduplication.</summary>
    public IEnumerable<string> EmailKeys => Emails.Select(EmailAddress.Normalise).OfType<string>();

    public IEnumerable<string> PhoneKeys => Phones.Select(p => PhoneNumber.Normalise(p)).OfType<string>();
}

public static class ContactMerge
{
    /// <summary>
    /// Merges two records of one person. The result keeps each address, number, and tag of both. The older record
    /// gives the ID and the creation date, and a tie goes to the lower ID. The record with the latest activity gives
    /// the name, and a tie goes to the name that sorts first. Thus the order of the arguments, and the order of a
    /// chain of merges, do not change the result.
    /// </summary>
    public static Contact Merge(Contact a, Contact b)
    {
        var older = Older(a, b) ? a : b;
        var recent = Recent(a, b) ? a : b;

        return new Contact
        {
            Id = older.Id,
            CreatedOn = older.CreatedOn,
            FirstName = recent.FirstName,
            LastName = recent.LastName,
            LastActivityOn = recent.LastActivityOn,
            Emails = Union(a.Emails, b.Emails, EmailAddress.Normalise),
            Phones = Union(a.Phones, b.Phones, p => PhoneNumber.Normalise(p)),
            Tags = Union(a.Tags, b.Tags, t => t.Trim().ToLowerInvariant() is { Length: > 0 } tag ? tag : null),
        };
    }

    private static bool Older(Contact a, Contact b) =>
        a.CreatedOn != b.CreatedOn ? a.CreatedOn < b.CreatedOn : a.Id.CompareTo(b.Id) <= 0;

    // The tie-break uses the fields that the merged record takes from the winner, and not the ID: the ID of a
    // merged record comes from a different record, and a tie-break on it gives a different result for (a + b) + c
    // and a + (b + c).
    private static bool Recent(Contact a, Contact b)
    {
        if (a.LastActivityOn != b.LastActivityOn)
        {
            return a.LastActivityOn > b.LastActivityOn;
        }

        var byLastName = string.CompareOrdinal(a.LastName, b.LastName);
        return byLastName != 0 ? byLastName < 0 : string.CompareOrdinal(a.FirstName, b.FirstName) <= 0;
    }

    /// <summary>The normalised values of both lists, each one time, in ordinal order. A value that does not normalise
    /// stays as it is, so that a merge never loses data.</summary>
    private static IReadOnlyList<string> Union(IEnumerable<string> a, IEnumerable<string> b, Func<string, string?> normalise) =>
        a.Concat(b)
            .Select(v => normalise(v) ?? v)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}

public static class Deduplicator
{
    /// <summary>
    /// Merges each group of contacts that share a normalised email address or phone number. Sharing is transitive:
    /// if A shares an email with B, and B shares a phone with C, then A, B, and C are one person.
    /// </summary>
    public static IReadOnlyList<Contact> Deduplicate(IReadOnlyList<Contact> contacts)
    {
        var parent = Enumerable.Range(0, contacts.Count).ToArray();
        int Find(int i) => parent[i] == i ? i : parent[i] = Find(parent[i]);

        var owner = new Dictionary<string, int>();
        for (var i = 0; i < contacts.Count; i++)
        {
            var keys = contacts[i].EmailKeys.Select(e => "email:" + e).Concat(contacts[i].PhoneKeys.Select(p => "phone:" + p));
            foreach (var key in keys)
            {
                if (owner.TryGetValue(key, out var other))
                {
                    parent[Find(i)] = Find(other);
                }
                else
                {
                    owner[key] = i;
                }
            }
        }

        return Enumerable.Range(0, contacts.Count)
            .GroupBy(Find)
            .Select(group => group.Select(i => contacts[i]).Aggregate(ContactMerge.Merge))
            .OrderBy(c => c.Id)
            .ToList();
    }
}
