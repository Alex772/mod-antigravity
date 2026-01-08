using System.Collections.Generic;

namespace Antigravity.Core.Commands
{
    /// <summary>
    /// Command for synchronizing element changes (gas/liquid placement, removal, etc.)
    /// </summary>
    public class ElementChangeCommand : GameCommand
    {
        /// <summary>
        /// Batch of element changes to apply
        /// </summary>
        public List<ElementDelta> Changes { get; set; } = new List<ElementDelta>();
        
        public ElementChangeCommand() : base(GameCommandType.ElementChange) { }
    }
    
    /// <summary>
    /// Represents a single cell element change
    /// </summary>
    public struct ElementDelta
    {
        /// <summary>
        /// Cell index in the Grid
        /// </summary>
        public int Cell;
        
        /// <summary>
        /// Element ID (from SimHashes)
        /// </summary>
        public int ElementId;
        
        /// <summary>
        /// Mass in kg
        /// </summary>
        public float Mass;
        
        /// <summary>
        /// Temperature in Kelvin
        /// </summary>
        public float Temperature;
        
        /// <summary>
        /// Type of change
        /// </summary>
        public ElementChangeType ChangeType;
        
        /// <summary>
        /// True if element is a gas, false if liquid/solid
        /// </summary>
        public bool IsGas;
        
        /// <summary>
        /// True if element is a liquid
        /// </summary>
        public bool IsLiquid;
    }
    
    /// <summary>
    /// Type of element change
    /// </summary>
    public enum ElementChangeType : byte
    {
        /// <summary>Add/place element</summary>
        Add = 0,
        
        /// <summary>Remove/dig element</summary>
        Remove = 1,
        
        /// <summary>Replace element (e.g., gas to liquid)</summary>
        Replace = 2,
        
        /// <summary>Modify element properties (mass/temp change)</summary>
        Modify = 3
    }
}
