using GalaxyCheck;

namespace Crm.Tests;

[NUnit.Framework.TestFixture]
public class CompanyProperties
{
    public static IGen<Company> Groups => CrmGen.CompanyGroup;

    // Recursive data: the cover rules make sure that the run tries real trees, not only single companies.
    [Property]
    public Property GroupEmployeesAreTheSumOverTheWholeTree() =>
        Property.ForAll(Groups, group => group.GroupEmployees == group.Flatten().Sum(c => c.Employees))
            .Cover("a single company", 5, group => group.Depth == 0)
            .Cover("two or more levels below the top", 20, group => group.Depth >= 2);

    [Property]
    public bool AGroupIsNeverInASmallerSegmentThanItsMembers([MemberGen(nameof(Groups))] Company group) =>
        group.Flatten().All(member => member.Segment <= group.Segment);

    [Property]
    public bool FlattenListsEachCompanyOneTime([MemberGen(nameof(Groups))] Company group) =>
        group.Flatten().Count() == 1 + group.Subsidiaries.Sum(s => s.Flatten().Count());
}

[NUnit.Framework.TestFixture]
public class LeadScoreProperties
{
    private static readonly DateOnly Today = new(2026, 6, 30);

    public static IGen<Contact> Contacts => CrmGen.Contact;

    public static IGen<Company?> Companies => CrmGen.CompanyGroup.OrNull();

    public static IGen<IReadOnlyList<Activity>> Activities => CrmGen.ActivityOn(CrmGen.Day).ListOf().WithCountLessThanEqual(15);

    public static IGen<Activity> OneActivity => CrmGen.ActivityOn(CrmGen.Day);

    [Property]
    public bool ScoreIsFrom0To100(
        [MemberGen(nameof(Contacts))] Contact contact,
        [MemberGen(nameof(Companies))] Company? company,
        [MemberGen(nameof(Activities))] IReadOnlyList<Activity> activities)
    {
        var score = LeadScore.Score(contact, company, activities, Today);
        return score is >= 0 and <= 100;
    }

    // Metamorphic: one more activity never makes a lead less interesting.
    [Property]
    public bool AnotherActivityNeverLowersTheScore(
        [MemberGen(nameof(Contacts))] Contact contact,
        [MemberGen(nameof(Companies))] Company? company,
        [MemberGen(nameof(Activities))] IReadOnlyList<Activity> activities,
        [MemberGen(nameof(OneActivity))] Activity extra) =>
        LeadScore.Score(contact, company, [.. activities, extra], Today) >= LeadScore.Score(contact, company, activities, Today);

    // Metamorphic: an activity after the day of the score does not count.
    [Property]
    public bool FutureActivitiesDoNotCount(
        [MemberGen(nameof(Contacts))] Contact contact,
        [MemberGen(nameof(Activities))] IReadOnlyList<Activity> activities)
    {
        var past = activities.Where(a => a.On <= Today).ToList();
        return LeadScore.Score(contact, null, activities, Today) == LeadScore.Score(contact, null, past, Today);
    }
}

[NUnit.Framework.TestFixture]
public class PipelineProperties
{
    // A small domain: 6 stages, so 36 pairs. The run tries each pair one time (an exhaustive run), not 100 random ones.
    [Property]
    public bool AnOpenDealNeverSkipsAStage(Stage from, Stage to)
    {
        Property.Precondition(Pipeline.IsOpen(from) && Pipeline.IsOpen(to));
        return !Pipeline.CanMove(from, to) || Math.Abs((int)to - (int)from) == 1;
    }

    [Property]
    public bool AWonDealIsFinal(Stage to) => !Pipeline.CanMove(Stage.Won, to);

    [Property]
    public bool OnlyNegotiationLeadsToWon(Stage from) => Pipeline.CanMove(from, Stage.Won) == (from == Stage.Negotiation);

    [Property]
    public bool EachStageExceptWonCanStillReachWon(Stage start)
    {
        Property.Precondition(start != Stage.Won);
        var seen = new HashSet<Stage> { start };
        var queue = new Queue<Stage>([start]);
        while (queue.TryDequeue(out var stage))
        {
            foreach (var next in Enum.GetValues<Stage>().Where(n => Pipeline.CanMove(stage, n) && seen.Add(n)))
            {
                queue.Enqueue(next);
            }
        }

        return seen.Contains(Stage.Won);
    }

    // The result of a run tells if the run was exhaustive.
    [NUnit.Framework.Test]
    public void TheStageRulesAreCheckedExhaustively()
    {
        var result = Property.ForAll(Gen.Enum<Stage>(), Gen.Enum<Stage>(), (from, to) => AnOpenDealNeverSkipsAStage(from, to))
            .Check();
        NUnit.Framework.Assert.That(result.Falsified, NUnit.Framework.Is.False);
        NUnit.Framework.Assert.That(result.Exhaustive, NUnit.Framework.Is.True, result.Report);
    }

    public static IGen<IReadOnlyList<Stage>> Attempts => Gen.Enum<Stage>().ListOf().WithCountLessThanEqual(20);

    // Invariant: whatever moves people try, the history of a deal holds only allowed moves.
    [Property]
    public bool DealHistoryHoldsOnlyAllowedMoves([MemberGen(nameof(Attempts))] IReadOnlyList<Stage> attempts)
    {
        var deal = new Deal("Acme renewal");
        foreach (var stage in attempts)
        {
            try
            {
                deal.MoveTo(stage);
            }
            catch (InvalidStageMoveException)
            {
                // A refused move is a normal result here.
            }
        }

        return deal.History[0] == Stage.Lead && deal.History.Zip(deal.History.Skip(1)).All(p => Pipeline.CanMove(p.First, p.Second));
    }
}

[NUnit.Framework.TestFixture]
public class SearchProperties
{
    public static IGen<IReadOnlyList<Contact>> Directories => CrmGen.UniqueIds(CrmGen.Contact.ListOf().WithCountLessThanEqual(30));

    /// <summary>Queries of one or two short words, from the letters that start most names.</summary>
    public static IGen<string> Queries =>
        Gen.String().FromCharacters("abcdejklmnrst").WithLengthBetween(1, 3).ListOf().WithCountBetween(1, 2)
            .Select(words => string.Join(" ", words));

    private static bool Matches(Contact contact, string query) =>
        ContactIndex.Words(query).All(q => ContactIndex.WordsOf(contact).Any(w => w.StartsWith(q, StringComparison.Ordinal)));

    // Oracle: the index finds the same contacts as a scan of each contact.
    [Property]
    public Property SearchAgreesWithALinearScan() =>
        Property.ForAll(Directories, Queries, (contacts, query) =>
                new ContactIndex(contacts).Search(query).SequenceEqual(contacts.Where(c => Matches(c, query))))
            .Cover("the query finds something", 30, x => x.Item1.Any(c => Matches(c, x.Item2)));

    [Property]
    public bool EachContactIsFoundByItsFullName([MemberGen(nameof(Directories))] IReadOnlyList<Contact> contacts) =>
        contacts.All(c => new ContactIndex(contacts).Search(c.FullName).Contains(c));

    // Metamorphic: one more word in the query can only remove results.
    [Property]
    public bool AnotherWordNeverAddsResults(
        [MemberGen(nameof(Directories))] IReadOnlyList<Contact> contacts,
        [MemberGen(nameof(Queries))] string query,
        [MemberGen(nameof(Queries))] string word)
    {
        var index = new ContactIndex(contacts);
        return index.Search(query + " " + word).All(index.Search(query).Contains);
    }
}
