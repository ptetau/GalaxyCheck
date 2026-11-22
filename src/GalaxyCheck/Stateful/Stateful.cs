using System;
using System.Collections.Generic;

namespace GalaxyCheck.Stateful
{
    /// <summary>One step of a stateful test, against a real system and a model of it.</summary>
    public abstract class Command<TSystem, TModel>
    {
        public virtual bool Precondition(TModel model) => true;

        public abstract void Run(TSystem system);

        public abstract TModel NextModel(TModel model);

        public virtual bool Postcondition(TSystem system, TModel modelBefore, TModel modelAfter) => true;

        public virtual bool Expects(Exception exception) => false;
    }

    public static class StatefulGen
    {
        public static IGen<IReadOnlyList<Command<TSystem, TModel>>> Commands<TSystem, TModel>(
            Func<TModel> newModel,
            Func<TModel, IGen<Command<TSystem, TModel>>> nextCommand) => throw new System.NotImplementedException("not implemented");
    }

    public static class StatefulProperty
    {
        public static Property<IReadOnlyList<Command<TSystem, TModel>>> Create<TSystem, TModel>(
            Func<TSystem> newSystem,
            Func<TModel> newModel,
            Func<TModel, IGen<Command<TSystem, TModel>>> nextCommand) => throw new System.NotImplementedException("not implemented");
    }
}
