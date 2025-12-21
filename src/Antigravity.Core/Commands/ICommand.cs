using System;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Interface for all game commands that can be synchronized.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Unique identifier for this command type.
        /// </summary>
        byte CommandId { get; }

        /// <summary>
        /// Tick when this command was created.
        /// </summary>
        int Tick { get; set; }

        /// <summary>
        /// ID of the player who issued this command.
        /// </summary>
        int PlayerId { get; set; }

        /// <summary>
        /// Timestamp when the command was created.
        /// </summary>
        long Timestamp { get; set; }

        /// <summary>
        /// Execute this command on the local game state.
        /// </summary>
        void Execute();

        /// <summary>
        /// Undo this command (if possible).
        /// </summary>
        void Undo();

        /// <summary>
        /// Validate that this command can be executed.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        bool Validate();

        /// <summary>
        /// Serialize this command to bytes.
        /// </summary>
        byte[] Serialize();

        /// <summary>
        /// Deserialize command data from bytes.
        /// </summary>
        void Deserialize(byte[] data);
    }
}
