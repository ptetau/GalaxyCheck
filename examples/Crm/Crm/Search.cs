namespace Crm;

/// <summary>
/// A search index of contacts. A query is a list of words. A contact matches if each word of the query is the start
/// of a word of the contact: of its first name, its last name, or the local part of an email address. The match
/// ignores case. The query "ann smi" finds "Ann Smith" and "Annabel Smithers".
/// </summary>
public sealed class ContactIndex
{
    // Each word, with the positions of the contacts that have it, in ordinal order for a prefix search.
    private readonly (string Word, int Contact)[] _entries;
    private readonly IReadOnlyList<Contact> _contacts;

    public ContactIndex(IReadOnlyList<Contact> contacts)
    {
        _contacts = contacts;
        _entries = contacts
            .SelectMany((contact, i) => WordsOf(contact).Select(word => (word, i)))
            .Distinct()
            .OrderBy(e => e.word, StringComparer.Ordinal)
            .ThenBy(e => e.i)
            .ToArray();
    }

    public static IEnumerable<string> WordsOf(Contact contact) =>
        Words(contact.FirstName)
            .Concat(Words(contact.LastName))
            .Concat(contact.EmailKeys.SelectMany(e => Words(e[..e.IndexOf('@')])));

    /// <summary>Splits text into words in lower case, at each character that is not a letter or a digit.</summary>
    public static IEnumerable<string> Words(string text) =>
        text.ToLowerInvariant().Split(c => !char.IsLetterOrDigit(c));

    public IReadOnlyList<Contact> Search(string query)
    {
        var words = Words(query).Distinct().ToList();
        if (words.Count == 0)
        {
            return [];
        }

        IEnumerable<int>? matches = null;
        foreach (var word in words)
        {
            var found = WithPrefix(word);
            matches = matches is null ? found : matches.Intersect(found);
        }

        return matches!.Order().Select(i => _contacts[i]).ToList();
    }

    /// <summary>The contacts that have a word that starts with the prefix. The entries with a prefix are next to each
    /// other in ordinal order, so a binary search finds the first, and a scan reads the rest.</summary>
    private HashSet<int> WithPrefix(string prefix)
    {
        int low = 0, high = _entries.Length;
        while (low < high)
        {
            var mid = (low + high) / 2;
            if (string.CompareOrdinal(_entries[mid].Word, prefix) < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        var found = new HashSet<int>();
        for (var i = low; i < _entries.Length && _entries[i].Word.StartsWith(prefix, StringComparison.Ordinal); i++)
        {
            found.Add(_entries[i].Contact);
        }

        return found;
    }
}

internal static class SplitExtensions
{
    public static IEnumerable<string> Split(this string text, Func<char, bool> isSeparator)
    {
        var start = 0;
        for (var i = 0; i <= text.Length; i++)
        {
            if (i == text.Length || isSeparator(text[i]))
            {
                if (i > start)
                {
                    yield return text[start..i];
                }

                start = i + 1;
            }
        }
    }
}
