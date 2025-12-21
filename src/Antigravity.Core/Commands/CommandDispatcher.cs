using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Dispatches and manages game commands for synchronization.
    /// </summary>
    public static class CommandDispatcher
    {
        /// <summary>
        /// Queue of pending commands to be executed.
        /// </summary>
        private static readonly ConcurrentQueue<ICommand> PendingCommands = new ConcurrentQueue<ICommand>();

        /// <summary>
        /// History of executed commands (for potential rollback).
        /// </summary>
        private static readonly List<ICommand> CommandHistory = new List<ICommand>();

        /// <summary>
        /// Maximum commands to keep in history.
        /// </summary>
        private const int MaxHistorySize = 1000;

        /// <summary>
        /// Event fired when a command is dispatched.
        /// </summary>
        public static event Action<ICommand> OnCommandDispatched;

        /// <summary>
        /// Event fired when a command is executed.
        /// </summary>
        public static event Action<ICommand> OnCommandExecuted;

        /// <summary>
        /// Whether the dispatcher is initialized.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Initialize the command dispatcher.
        /// </summary>
        public static void Initialize()
        {
            IsInitialized = true;
        }

        /// <summary>
        /// Dispatch a command to be synchronized and executed.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        public static void Dispatch(ICommand command)
        {
            if (!IsInitialized) return;
            if (command == null) return;
            if (!command.Validate()) return;

            // Set timestamp
            command.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Add to pending queue
            PendingCommands.Enqueue(command);

            // Notify listeners
            OnCommandDispatched?.Invoke(command);
        }

        /// <summary>
        /// Process and execute all pending commands.
        /// </summary>
        public static void ProcessPendingCommands()
        {
            while (PendingCommands.TryDequeue(out var command))
            {
                ExecuteCommand(command);
            }
        }

        /// <summary>
        /// Execute a single command.
        /// </summary>
        private static void ExecuteCommand(ICommand command)
        {
            try
            {
                command.Execute();

                // Add to history
                CommandHistory.Add(command);

                // Trim history if needed
                if (CommandHistory.Count > MaxHistorySize)
                {
                    CommandHistory.RemoveRange(0, CommandHistory.Count - MaxHistorySize);
                }

                // Notify listeners
                OnCommandExecuted?.Invoke(command);
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                UnityEngine.Debug.LogError($"[Antigravity] Failed to execute command: {ex.Message}");
            }
        }

        /// <summary>
        /// Receive a command from the network and queue it for execution.
        /// </summary>
        public static void ReceiveCommand(ICommand command)
        {
            if (!IsInitialized) return;
            if (command == null) return;

            PendingCommands.Enqueue(command);
        }

        /// <summary>
        /// Get the number of pending commands.
        /// </summary>
        public static int PendingCount => PendingCommands.Count;

        /// <summary>
        /// Clear all pending commands.
        /// </summary>
        public static void ClearPending()
        {
            while (PendingCommands.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Clear command history.
        /// </summary>
        public static void ClearHistory()
        {
            CommandHistory.Clear();
        }

        /// <summary>
        /// Shutdown the dispatcher.
        /// </summary>
        public static void Shutdown()
        {
            ClearPending();
            ClearHistory();
            IsInitialized = false;
        }
    }
}
