namespace Crm;

/// <summary>A company, with its subsidiaries. A group of companies is a tree.</summary>
public sealed record Company(string Name, int Employees, IReadOnlyList<Company> Subsidiaries)
{
    public static Company Single(string name, int employees) => new(name, employees, []);

    /// <summary>The company and each company below it, parent first.</summary>
    public IEnumerable<Company> Flatten() => Subsidiaries.SelectMany(s => s.Flatten()).Prepend(this);

    public int GroupEmployees => Employees + Subsidiaries.Sum(s => s.GroupEmployees);

    public int Depth => Subsidiaries.Count == 0 ? 0 : 1 + Subsidiaries.Max(s => s.Depth);

    public SizeSegment Segment => SizeSegments.Of(GroupEmployees);
}

public enum SizeSegment
{
    Micro,
    Small,
    Medium,
    Enterprise,
}

public static class SizeSegments
{
    /// <summary>The segment of a count of employees: below 10, below 50, below 250, and 250 or more.</summary>
    public static SizeSegment Of(int employees) => employees switch
    {
        < 0 => throw new ArgumentOutOfRangeException(nameof(employees)),
        < 10 => SizeSegment.Micro,
        < 50 => SizeSegment.Small,
        < 250 => SizeSegment.Medium,
        _ => SizeSegment.Enterprise,
    };
}

public sealed record Activity(DateOnly On, string Kind);

public static class LeadScore
{
    /// <summary>
    /// A score from 0 to 100 that tells sales whom to call first. The day of the score is an argument, so that the
    /// score never depends on the clock.
    /// </summary>
    public static int Score(Contact contact, Company? company, IReadOnlyList<Activity> activities, DateOnly today)
    {
        var score = 0;
        score += contact.EmailKeys.Any() ? 20 : 0;
        score += contact.PhoneKeys.Any() ? 10 : 0;
        score += company?.Segment switch
        {
            SizeSegment.Small => 10,
            SizeSegment.Medium => 20,
            SizeSegment.Enterprise => 30,
            _ => 0,
        };

        var recent = activities.Count(a => a.On <= today && a.On > today.AddDays(-30));
        score += Math.Min(recent * 5, 30);
        score += activities.Any(a => a.Kind == "meeting" && a.On <= today && a.On > today.AddDays(-90)) ? 15 : 0;

        var lastSeen = activities.Where(a => a.On <= today).Select(a => (DateOnly?)a.On).Max();
        if (lastSeen is null || lastSeen.Value < today.AddDays(-180))
        {
            score -= 25;
        }

        return Math.Clamp(score, 0, 100);
    }
}

public enum Stage
{
    Lead,
    Qualified,
    Proposal,
    Negotiation,
    Won,
    Lost,
}

public sealed class InvalidStageMoveException(Stage from, Stage to) : InvalidOperationException($"A deal cannot move from {from} to {to}.");

public static class Pipeline
{
    public static bool IsOpen(Stage stage) => stage is not (Stage.Won or Stage.Lost);

    /// <summary>
    /// The rules of the sales pipeline. An open deal moves one stage forward or one stage back, or is lost. Only a
    /// deal in negotiation can be won. A won deal is final. A lost deal can only be reopened as a lead.
    /// </summary>
    public static bool CanMove(Stage from, Stage to)
    {
        if (from == to)
        {
            return false;
        }

        return from switch
        {
            Stage.Won => false,
            Stage.Lost => to == Stage.Lead,
            Stage.Negotiation when to == Stage.Won => true,
            _ when to == Stage.Lost => true,
            _ => IsOpen(to) && Math.Abs((int)to - (int)from) == 1,
        };
    }
}

/// <summary>A deal that moves through the pipeline, with the history of its stages.</summary>
public sealed class Deal(string name)
{
    private readonly List<Stage> _history = [Stage.Lead];

    public string Name { get; } = name;

    public Stage Stage => _history[^1];

    public IReadOnlyList<Stage> History => _history;

    public void MoveTo(Stage stage)
    {
        if (!Pipeline.CanMove(Stage, stage))
        {
            throw new InvalidStageMoveException(Stage, stage);
        }

        _history.Add(stage);
    }
}
