using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>
        /// Saves the state of the random number generator at the start of the value. Each generator inside that uses
        /// <see cref="ReferenceRngWaypoint{T}(IGenAdvanced{T}, int)"/> draws its random values from a state that only
        /// depends on the waypoint, so that its value does not change when a sibling generator changes.
        /// </summary>
        public static IGen<T> SetRngWaypoint<T>(this IGenAdvanced<T> gen) => new SetRngWaypointGen<T>(gen);

        /// <summary>
        /// Draws the random values of the generator from a state that only depends on the current waypoint and the
        /// given influence.
        /// </summary>
        public static IGen<T> ReferenceRngWaypoint<T>(this IGenAdvanced<T> gen, int influence) =>
            new ReferenceRngWaypointGen<T>(gen, influence);
    }
}

namespace GalaxyCheck.Gens.Internal
{
    internal sealed record SetRngWaypointGen<T>(IGenAdvanced<T> Source) : BaseGen<T>
    {
        public override T Generate(GenSource source)
        {
            var previous = source.SetWaypoint();
            try
            {
                return Source.Generate(source);
            }
            finally
            {
                source.RestoreWaypoint(previous);
            }
        }
    }

    internal sealed record ReferenceRngWaypointGen<T>(IGenAdvanced<T> Source, int Influence) : BaseGen<T>
    {
        public override T Generate(GenSource source)
        {
            var saved = source.EnterWaypoint(Influence);
            try
            {
                return Source.Generate(source);
            }
            finally
            {
                source.ExitWaypoint(saved);
            }
        }
    }
}
