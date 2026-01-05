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
        
        // Mop/Clean commands
        Mop = 80,
        Clear = 81,
        
        // Harvest commands
        Harvest = 82,
        CancelHarvest = 83,
        
        // Disinfect
        Disinfect = 84,
        
        // Capture commands
        Capture = 85,
        CancelCapture = 86,
        
        // Bulk priority (PrioritizeTool)
        SetBulkPriority = 87,
        
        // Building-level commands (via UI sidebar, not tools)
        SetBuildingPriority = 88,      // Prioridade via aba de propriedades
        ToggleBuildingDisinfect = 89,  // Toggle desinfecção via UI
        SetStorageFilter = 90,          // Filtros de storage bin
        
        // Utility build (wires, pipes - uses path instead of single cell)
        UtilityBuild = 91,
        
        // Disconnect utility (cut wires/pipes)
        DisconnectUtility = 92,
        
        // Storage capacity
        SetStorageCapacity = 93,
        
        // Duplicant synchronization
        ChoreStart = 110,           // Duplicant started a chore
        ChoreEnd = 111,             // Duplicant ended a chore
        NavigateTo = 112,           // Duplicant navigation destination
        DuplicantFullState = 113,   // Full state sync (fallback)
        DuplicantChecksum = 114,    // Checksum verification
        PositionSync = 115,         // Lightweight position sync
        RandomSeedSync = 116,       // Random seed synchronization
        DuplicantCommandRequest = 117, // Client requests action from Host
        
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
    /// Utility build command - for wires, pipes (sends entire path)
    /// </summary>
    [Serializable]
    public class UtilityBuildCommand : GameCommand
    {
        public string BuildingDefId { get; set; }
        public int[] PathCells { get; set; }
        public string[] SelectedElements { get; set; }
        public string FacadeId { get; set; }

        public UtilityBuildCommand() : base(GameCommandType.UtilityBuild) { }
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
        
        /// <summary>
        /// Active filter layers (e.g., "WIRES", "BUILDINGS", "ALL").
        /// Null or empty means ALL.
        /// </summary>
        public string[] ActiveFilterLayers { get; set; }

        public DeconstructCommand() : base(GameCommandType.Deconstruct) { }
    }

    /// <summary>
    /// Mop command - for MopTool
    /// </summary>
    [Serializable]
    public class MopCommand : GameCommand
    {
        public int[] Cells { get; set; }
        public int Priority { get; set; }

        public MopCommand() : base(GameCommandType.Mop) { }
    }

    /// <summary>
    /// Clear command - for ClearTool
    /// </summary>
    [Serializable]
    public class ClearCommand : GameCommand
    {
        public int[] Cells { get; set; }
        public int Priority { get; set; }

        public ClearCommand() : base(GameCommandType.Clear) { }
    }

    /// <summary>
    /// Harvest command - for HarvestTool
    /// </summary>
    [Serializable]
    public class HarvestCommand : GameCommand
    {
        public int[] Cells { get; set; }
        public bool HarvestWhenReady { get; set; }
        public int Priority { get; set; }

        public HarvestCommand() : base(GameCommandType.Harvest) { }
    }

    /// <summary>
    /// Disinfect command - for DisinfectTool
    /// </summary>
    [Serializable]
    public class DisinfectCommand : GameCommand
    {
        public int[] Cells { get; set; }

        public DisinfectCommand() : base(GameCommandType.Disinfect) { }
    }

    /// <summary>
    /// Capture command - for CaptureTool (uses area, not cells)
    /// </summary>
    [Serializable]
    public class CaptureCommand : GameCommand
    {
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public bool Mark { get; set; }
        public int PriorityClass { get; set; }
        public int PriorityValue { get; set; }

        public CaptureCommand() : base(GameCommandType.Capture) { }
    }

    /// <summary>
    /// Bulk priority command - for PrioritizeTool
    /// </summary>
    [Serializable]
    public class BulkPriorityCommand : GameCommand
    {
        public int[] Cells { get; set; }
        public int PriorityClass { get; set; }
        public int PriorityValue { get; set; }

        public BulkPriorityCommand() : base(GameCommandType.SetBulkPriority) { }
    }

    /// <summary>
    /// Door state command - for Door control
    /// </summary>
    [Serializable]
    public class DoorStateCommand : GameCommand
    {
        public int Cell { get; set; }
        public int ControlState { get; set; } // 0=Auto, 1=Open, 2=Locked

        public DoorStateCommand() : base(GameCommandType.SetDoorState) { }
    }

    /// <summary>
    /// Building priority command - for priority changes via UI sidebar (not PrioritizeTool)
    /// </summary>
    [Serializable]
    public class BuildingPriorityCommand : GameCommand
    {
        public int Cell { get; set; }
        public int PriorityClass { get; set; }
        public int PriorityValue { get; set; }

        public BuildingPriorityCommand() : base(GameCommandType.SetBuildingPriority) { }
    }

    /// <summary>
    /// Building disinfect command - for toggle disinfect via UI sidebar
    /// </summary>
    [Serializable]
    public class BuildingDisinfectCommand : GameCommand
    {
        public int Cell { get; set; }
        public bool MarkForDisinfect { get; set; }

        public BuildingDisinfectCommand() : base(GameCommandType.ToggleBuildingDisinfect) { }
    }

    /// <summary>
    /// Storage filter command - for filter changes via TreeFilterable UI
    /// </summary>
    [Serializable]
    public class StorageFilterCommand : GameCommand
    {
        public int Cell { get; set; }
        public string[] AcceptedTags { get; set; }

        public StorageFilterCommand() : base(GameCommandType.SetStorageFilter) { }
    }

    /// <summary>
    /// Storage capacity command - for max capacity changes via UI slider
    /// </summary>
    [Serializable]
    public class StorageCapacityCommand : GameCommand
    {
        public int Cell { get; set; }
        public float UserMaxCapacity { get; set; }

        public StorageCapacityCommand() : base(GameCommandType.SetStorageCapacity) { }
    }

    /// <summary>
    /// Disconnect utility command - for cutting wire/pipe connections
    /// </summary>
    [Serializable]
    public class DisconnectCommand : GameCommand
    {
        public int Cell { get; set; }
        public int RemoveConnections { get; set; }  // UtilityConnections flags cast to int
        public string FilterLayer { get; set; }     // WIRES, LIQUIDCONDUIT, GASCONDUIT, SOLIDCONDUIT, LOGIC

        public DisconnectCommand() : base(GameCommandType.DisconnectUtility) { }
    }

    /// <summary>
    /// Command sent when a Duplicant starts a chore.
    /// </summary>
    [Serializable]
    public class ChoreStartCommand : GameCommand
    {
        public string DuplicantName { get; set; }
        public string ChoreTypeId { get; set; }
        public int TargetCell { get; set; }
        
        // InstanceID of target GameObject (e.g. what is being dug or built)
        public int TargetId { get; set; } 
        
        // Serialized context data if needed
        public string ContextData { get; set; }

        public ChoreStartCommand() : base(GameCommandType.ChoreStart) { }
    }

    /// <summary>
    /// Command sent when a Duplicant ends a chore.
    /// </summary>
    [Serializable]
    public class ChoreEndCommand : GameCommand
    {
        public string DuplicantName { get; set; }
        public string ChoreTypeId { get; set; }

        public ChoreEndCommand() : base(GameCommandType.ChoreEnd) { }
    }

    /// <summary>
    /// Command sent when a Duplicant sets a navigation destination.
    /// </summary>
    [Serializable]
    public class NavigateToCommand : GameCommand
    {
        public string DuplicantName { get; set; }
        public int TargetCell { get; set; }
        public int TargetId { get; set; } // Optional target object

        public NavigateToCommand() : base(GameCommandType.NavigateTo) { }
    }

    /// <summary>
    /// Command to verify Duplicant state consistency.
    /// </summary>
    [Serializable]
    public class DuplicantChecksumCommand : GameCommand
    {
        public string DuplicantName { get; set; }
        public long Checksum { get; set; }
        public int CurrentCell { get; set; }
        public string CurrentChore { get; set; }

        public DuplicantChecksumCommand() : base(GameCommandType.DuplicantChecksum) { }
    }

    /// <summary>
    /// Full state synchronization for a Duplicant (fallback mechanism).
    /// </summary>
    [Serializable]
    public class DuplicantFullStateCommand : GameCommand
    {
        public string DuplicantName { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int CurrentCell { get; set; }
        public string CurrentChore { get; set; }
        public int TargetCell { get; set; }

        public DuplicantFullStateCommand() : base(GameCommandType.DuplicantFullState) { }
    }

    /// <summary>
    /// Lightweight position sync for a single Duplicant.
    /// Sent more frequently than full state for smoother sync.
    /// </summary>
    [Serializable]
    public class PositionSyncCommand : GameCommand
    {
        public string DuplicantName { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int CurrentCell { get; set; }
        public bool IsMoving { get; set; }
        public int TargetCell { get; set; }

        public PositionSyncCommand() : base(GameCommandType.PositionSync) { }
    }

    /// <summary>
    /// Synchronize the Random seed to ensure deterministic behavior.
    /// </summary>
    [Serializable]
    public class RandomSeedSyncCommand : GameCommand
    {
        public int Seed { get; set; }
        public long GameTick { get; set; }

        public RandomSeedSyncCommand() : base(GameCommandType.RandomSeedSync) { }
    }

    /// <summary>
    /// Types of requests a client can send to the Host.
    /// </summary>
    public enum DuplicantRequestType
    {
        MoveTo = 0,         // Request Duplicant move to a cell
        AssignChore = 1,    // Request Duplicant do a specific chore
        CancelChore = 2,    // Request cancellation of current chore
        SetPriority = 3     // Request priority change
    }

    /// <summary>
    /// Command sent from CLIENT to HOST requesting an action on a Duplicant.
    /// The Host validates and executes the request, then syncs the result.
    /// </summary>
    [Serializable]
    public class DuplicantCommandRequestCommand : GameCommand
    {
        public string DuplicantName { get; set; }
        public DuplicantRequestType RequestType { get; set; }
        public int TargetCell { get; set; }      // For MoveTo
        public string ChoreTypeId { get; set; }  // For AssignChore
        public int TargetObjectId { get; set; }  // For AssignChore (target object)

        public DuplicantCommandRequestCommand() : base(GameCommandType.DuplicantCommandRequest) { }
    }
}
