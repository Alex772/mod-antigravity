using NUnit.Framework;
using Antigravity.Core.Commands;
using Antigravity.Core;

namespace Antigravity.Tests.Unit.Commands
{
    /// <summary>
    /// Unit tests for the CommandDispatcher class.
    /// </summary>
    [TestFixture]
    public class CommandDispatcherTests
    {
        [SetUp]
        public void Setup()
        {
            // Redirect logging to avoid Unity dependency
            Log.Handler = (msg) => { /* Do nothing in tests */ };
            // Initialize before each test
            CommandDispatcher.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup after each test
            CommandDispatcher.Shutdown();
            Log.Handler = null;
        }

        [Test]
        public void Initialize_ShouldSetIsInitializedToTrue()
        {
            // Assert
            Assert.IsTrue(CommandDispatcher.IsInitialized);
        }

        [Test]
        public void Shutdown_ShouldSetIsInitializedToFalse()
        {
            // Act
            CommandDispatcher.Shutdown();

            // Assert
            Assert.IsFalse(CommandDispatcher.IsInitialized);
        }

        [Test]
        public void Dispatch_NullCommand_ShouldNotThrow()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => CommandDispatcher.Dispatch(null));
        }

        [Test]
        public void PendingCount_InitiallyZero()
        {
            // Assert
            Assert.AreEqual(0, CommandDispatcher.PendingCount);
        }

        [Test]
        public void ClearPending_ShouldResetPendingCount()
        {
            // Arrange - Add some commands would go here
            
            // Act
            CommandDispatcher.ClearPending();

            // Assert
            Assert.AreEqual(0, CommandDispatcher.PendingCount);
        }

        // TODO: Add more tests with mock ICommand implementations
    }
}
