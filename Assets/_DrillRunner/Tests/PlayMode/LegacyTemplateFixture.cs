using UnityEngine;

namespace DrillRunner.Tests.PlayMode
{
    // Explicit fixture for tests that relocate the archived M2 Wood/Brick/Stone opening.
    // Ordinary-flow and encounter tests continue to use the short M7-B introduction.
    internal static class LegacyTemplateFixture
    {
        internal static TrackCatalog Restore(TrackStreamer track, PrototypeHud hud)
        {
            TrackCatalog owned = null;
            if (track.EncounterPlan != null) { owned = Object.Instantiate(track.Catalog); track.Catalog = owned; }
            hud.Restart(); return owned;
        }
    }
}
