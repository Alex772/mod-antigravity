using NUnit.Framework;
using Antigravity.Core.Sync;
using Antigravity.Core;

namespace Antigravity.Tests.Unit.Sync
{
    /// <summary>
    /// Unit tests for the SyncEngine class.
    /// </summary>
    [TestFixture]
    public class SyncEngineTests
    {
        [SetUp]
        public void Setup()
        {
            // Redirect logging to avoid Unity dependency
            Log.Handler = (msg) => { /* Do nothing in tests */ };
            SyncEngine.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            SyncEngine.Stop();
            Log.Handler = null;
        }

        [Test]
        public void Initialize_ShouldResetState()
        {
            // Assert
            Assert.AreEqual(0, SyncEngine.CurrentTick);
            Assert.AreEqual(0, SyncEngine.SyncErrorCount);
            Assert.IsFalse(SyncEngine.IsRunning);
        }

        [Test]
        public void Start_ShouldSetIsRunningToTrue()
        {
            // Act
            SyncEngine.Start();

            // Assert
            Assert.IsTrue(SyncEngine.IsRunning);
        }

        [Test]
        public void Stop_ShouldSetIsRunningToFalse()
        {
            // Arrange
            SyncEngine.Start();

            // Act
            SyncEngine.Stop();

            // Assert
            Assert.IsFalse(SyncEngine.IsRunning);
        }

        [Test]
        public void ProcessTick_WhenNotRunning_ShouldNotIncrementTick()
        {
            // Arrange
            int initialTick = SyncEngine.CurrentTick;

            // Act
            SyncEngine.ProcessTick();

            // Assert
            Assert.AreEqual(initialTick, SyncEngine.CurrentTick);
        }

        [Test]
        public void ProcessTick_WhenRunning_ShouldIncrementTick()
        {
            // Arrange
            SyncEngine.Start();
            int initialTick = SyncEngine.CurrentTick;

            // Act
            SyncEngine.ProcessTick();

            // Assert
            Assert.AreEqual(initialTick + 1, SyncEngine.CurrentTick);
        }

        [Test]
        public void ReportSyncError_ShouldIncrementSyncErrorCount()
        {
            // Arrange
            int initialCount = SyncEngine.SyncErrorCount;

            // Act
            SyncEngine.ReportSyncError("Test error");

            // Assert
            Assert.AreEqual(initialCount + 1, SyncEngine.SyncErrorCount);
        }

        // TODO: Add more tests for hard sync and checksum verification
    }
}
