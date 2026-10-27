using NUnit.Framework;

namespace DrillRunner.Tests.PlayMode
{
    // Covers every existing/new PlayMode fixture before any Gameplay scene is loaded.
    // Each service gets a fresh store; tests cannot touch Application.persistentDataPath.
    [SetUpFixture]
    public sealed class ProgressionTestIsolation
    {
        [OneTimeSetUp] public void Isolate() => WorkshopController.StoreFactory = () => new MemoryProgressStore();
        [OneTimeTearDown] public void Restore() => WorkshopController.StoreFactory = null;
    }
}
