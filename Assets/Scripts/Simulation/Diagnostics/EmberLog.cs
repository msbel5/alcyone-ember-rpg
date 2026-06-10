using System;

namespace EmberCrpg.Simulation.Diagnostics
{
    /// <summary>
    /// Engine-free logger factory (the project standard going forward): Simulation code logs through a sink
    /// the Presentation layer assigns once (UnityEngine.Debug.Log in game; the headless harness can route to
    /// the console or leave it silent). Tagged, no-op when no sink, zero Unity dependency — a SOLID seam
    /// between domain code and the engine.
    /// </summary>
    public static class EmberLog
    {
        public static Action<string> Sink;

        public static EmberLogger For(string tag) => new EmberLogger(tag);
    }

    public readonly struct EmberLogger
    {
        private readonly string _tag;

        public EmberLogger(string tag) { _tag = tag; }

        public void Info(string message) => EmberLog.Sink?.Invoke("[" + _tag + "] " + message);
        public void Warn(string message) => EmberLog.Sink?.Invoke("[" + _tag + "] WARN " + message);
    }
}
