using System;
using System.Collections.Generic;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Types of game commands that can be synchronized.
    /// </summary>
    public enum GameCommandType
    {
        // Building commands
        Build = 1,
        CancelBuild = 2,
        Deconstruct = 3,
        
        // Digging commands
        Dig = 10,
        CancelDig = 11,
        
        // Priority commands
        SetPriority = 20,
        
        // Errand/Task commands
        SetErrand = 30,
        CancelErrand = 31,
        
        // Door/Access
        SetDoorState = 40,
        
        // Copy settings
        CopySettings = 50,
        
        // Speed control
        SetGameSpeed = 60,
        PauseGame = 61,
        UnpauseGame = 62,
        
        // Save
        SaveGame = 70,
        
        // Generic
        Custom = 100
    }

    /// <summary>
    /// Base class for game commands that can be synchronized.
    /// </summary>
    [Serializable]
    public class GameCommand
    {
        public GameCommandType Type { get; set; }
        public ulong SenderSteamId { get; set; }
        public long GameTick { get; set; }
        public int[] TargetCells { get; set; }
        public string ExtraData { get; set; }
        
        public GameCommand() { }
        
        public GameCommand(GameCommandType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Build command data
    /// </summary>
    [Serializable]
    public class BuildCommand : GameCommand
    {
        public string BuildingDefId { get; set; }
        public int Cell { get; set; }
        public int Orientation { get; set; }
        public string[] SelectedElements { get; set; }
        public string FacadeId { get; set; }

        public BuildCommand() : base(GameCommandType.Build) { }
    }

    /// <summary>
    /// Dig command data
    /// </summary>
    [Serializable]
    public class DigCommand : GameCommand
    {
        public int[] Cells { get; set; }

        public DigCommand() : base(GameCommandType.Dig) { }
    }

    /// <summary>
    /// Set priority command
    /// </summary>
    [Serializable]
    public class PriorityCommand : GameCommand
    {
        public int Cell { get; set; }
        public int Priority { get; set; }

        public PriorityCommand() : base(GameCommandType.SetPriority) { }
    }

    /// <summary>
    /// Game speed command
    /// </summary>
    [Serializable]
    public class SpeedCommand : GameCommand
    {
        public int Speed { get; set; } // 0 = paused, 1 = normal, 2 = fast, 3 = super fast

        public SpeedCommand() : base(GameCommandType.SetGameSpeed) { }
    }

    /// <summary>
    /// Deconstruct command
    /// </summary>
    [Serializable]
    public class DeconstructCommand : GameCommand
    {
        public int[] Cells { get; set; }

        public DeconstructCommand() : base(GameCommandType.Deconstruct) { }
    }
}
