using GalaxyCheck;

namespace Crm.Tests;

[NUnit.Framework.TestFixture]
public class MergeProperties
{
    public static IGen<Contact> Contacts => CrmGen.Contact;

    public static IGen<IReadOnlyList<Contact>> Duplicates => CrmGen.ContactsWithDuplicates;

    // Record equality compares lists by reference, so the tests compare the content.
    internal static string Content(Contact c) =>
        $"{c.Id}|{c.FirstName}|{c.LastName}|{c.CreatedOn}|{c.LastActivityOn}|" +
        $"{string.Join(",", c.Emails)}|{string.Join(",", c.Phones)}|{string.Join(",", c.Tags)}";

    internal static IEnumerable<string> Keys(IEnumerable<Contact> contacts) =>
        contacts.SelectMany(c => c.EmailKeys.Concat(c.PhoneKeys)).Distinct().Order();

    // The order of the two records must not matter: two sync jobs can find the same pair in either order.
    [Property]
    public bool MergeIsCommutative([MemberGen(nameof(Contacts))] Contact a, [MemberGen(nameof(Contacts))] Contact b) =>
        Content(ContactMerge.Merge(a, b)) == Content(ContactMerge.Merge(b, a));

    [Property]
    public bool MergeIsAssociative(
        [MemberGen(nameof(Contacts))] Contact a,
        [MemberGen(nameof(Contacts))] Contact b,
        [MemberGen(nameof(Contacts))] Contact c) =>
        Content(ContactMerge.Merge(ContactMerge.Merge(a, b), c)) == Content(ContactMerge.Merge(a, ContactMerge.Merge(b, c)));

    // Idempotent: to merge the same duplicate again changes nothing.
    [Property]
    public bool MergingAgainChangesNothing([MemberGen(nameof(Contacts))] Contact a, [MemberGen(nameof(Contacts))] Contact b)
    {
        var merged = ContactMerge.Merge(a, b);
        return Content(ContactMerge.Merge(merged, b)) == Content(merged);
    }

    [Property]
    public bool MergeLosesNoAddressNumberOrTag([MemberGen(nameof(Contacts))] Contact a, [MemberGen(nameof(Contacts))] Contact b)
    {
        var merged = ContactMerge.Merge(a, b);
        var tags = a.Tags.Concat(b.Tags).Select(t => t.Trim().ToLowerInvariant());
        return Keys([a, b]).All(k => merged.EmailKeys.Contains(k) || merged.PhoneKeys.Contains(k))
            && tags.All(merged.Tags.Contains);
    }

    [Property]
    public bool MergeKeepsTheOldestIdAndTheNewestName(
        [MemberGen(nameof(Contacts))] Contact a,
        [MemberGen(nameof(Contacts))] Contact b)
    {
        Property.Precondition(a.CreatedOn != b.CreatedOn && a.LastActivityOn != b.LastActivityOn);
        var merged = ContactMerge.Merge(a, b);
        var oldest = a.CreatedOn < b.CreatedOn ? a : b;
        var newest = a.LastActivityOn > b.LastActivityOn ? a : b;
        return merged.Id == oldest.Id && merged.FullName == newest.FullName;
    }

    // Invariant: after deduplication, no two contacts share an email address or a phone number.
    [Property]
    public Property NoTwoContactsShareAKeyAfterDeduplication() =>
        Property.ForAll(Duplicates, contacts =>
            {
                var keys = Deduplicator.Deduplicate(contacts).SelectMany(c => c.EmailKeys.Concat(c.PhoneKeys).Distinct()).ToList();
                return keys.Count == keys.Distinct().Count();
            })
            .Cover("some contacts are duplicates", 50, contacts => Deduplicator.Deduplicate(contacts).Count < contacts.Count);

    [Property]
    public bool DeduplicationKeepsEveryAddressAndNumber([MemberGen(nameof(Duplicates))] IReadOnlyList<Contact> contacts) =>
        Keys(Deduplicator.Deduplicate(contacts)).SequenceEqual(Keys(contacts));

    [Property]
    public bool DeduplicationIsIdempotent([MemberGen(nameof(Duplicates))] IReadOnlyList<Contact> contacts)
    {
        var once = Deduplicator.Deduplicate(contacts);
        return Deduplicator.Deduplicate(once).Select(Content).SequenceEqual(once.Select(Content));
    }

    public static IGen<(IReadOnlyList<Contact> Contacts, IReadOnlyList<Contact> Shuffled)> Shuffles =>
        from contacts in CrmGen.ContactsWithDuplicates
        from sortKeys in Gen.Int32().ListOf().WithCount(contacts.Count)
        select (contacts, (IReadOnlyList<Contact>)contacts.Zip(sortKeys).OrderBy(x => x.Second).Select(x => x.First).ToList());

    // Metamorphic: the order in which the contacts come in does not change the result.
    [Property]
    public bool DeduplicationDoesNotDependOnTheOrder(
        [MemberGen(nameof(Shuffles))] (IReadOnlyList<Contact> Contacts, IReadOnlyList<Contact> Shuffled) input) =>
        Deduplicator.Deduplicate(input.Contacts).Select(Content)
            .SequenceEqual(Deduplicator.Deduplicate(input.Shuffled).Select(Content));

    // Oracle: the union-find agrees with a slow method: merge any two contacts that share a key, until none do.
    [Property]
    public bool DeduplicationAgreesWithPairwiseMerging([MemberGen(nameof(Duplicates))] IReadOnlyList<Contact> contacts)
    {
        var slow = contacts.ToList();
        bool merged;
        do
        {
            merged = false;
            for (var i = 0; i < slow.Count && !merged; i++)
            {
                for (var j = i + 1; j < slow.Count && !merged; j++)
                {
                    if (Keys([slow[i]]).Intersect(Keys([slow[j]])).Any())
                    {
                        slow[i] = ContactMerge.Merge(slow[i], slow[j]);
                        slow.RemoveAt(j);
                        merged = true;
                    }
                }
            }
        }
        while (merged);

        return Deduplicator.Deduplicate(contacts).Select(Content).SequenceEqual(slow.OrderBy(c => c.Id).Select(Content));
    }
}
