namespace Antigravity.Core.Sync
{
    /// <summary>
    /// Categories of world state for sync verification
    /// </summary>
    public enum WorldSyncCategory
    {
        /// <summary>Items on the ground (debris, resources)</summary>
        Pickupables = 1,
        
        /// <summary>Buildings and their states</summary>
        Buildings = 2,
        
        /// <summary>Duplicants (position, stats)</summary>
        Duplicants = 4,
        
        /// <summary>Conduit contents (pipes, wires)</summary>
        Conduits = 8,
        
        /// <summary>Gas state (sampled)</summary>
        Gases = 16,
        
        /// <summary>Liquid state (sampled)</summary>
        Liquids = 32,
        
        /// <summary>All categories</summary>
        All = Pickupables | Buildings | Duplicants | Conduits | Gases | Liquids
    }

    /// <summary>
    /// Checksums for each sync category
    /// </summary>
    public class WorldStateChecksums
    {
        public long Timestamp { get; set; }
        public int GameTick { get; set; }
        
        public long PickupablesChecksum { get; set; }
        public int PickupablesCount { get; set; }
        
        public long BuildingsChecksum { get; set; }
        public int BuildingsCount { get; set; }
        
        public long DuplicantsChecksum { get; set; }
        public int DuplicantsCount { get; set; }
        
        public long ConduitsChecksum { get; set; }
        
        public long GasesChecksum { get; set; }
        public long LiquidsChecksum { get; set; }

        /// <summary>
        /// Get checksum for specific category
        /// </summary>
        public long GetChecksum(WorldSyncCategory category)
        {
            return category switch
            {
                WorldSyncCategory.Pickupables => PickupablesChecksum,
                WorldSyncCategory.Buildings => BuildingsChecksum,
                WorldSyncCategory.Duplicants => DuplicantsChecksum,
                WorldSyncCategory.Conduits => ConduitsChecksum,
                WorldSyncCategory.Gases => GasesChecksum,
                WorldSyncCategory.Liquids => LiquidsChecksum,
                _ => 0
            };
        }

        /// <summary>
        /// Compare with another checksum set with tolerance
        /// </summary>
        public WorldSyncCategory GetMismatchedCategories(WorldStateChecksums other)
        {
            WorldSyncCategory mismatched = 0;
            
            if (PickupablesChecksum != other.PickupablesChecksum)
                mismatched |= WorldSyncCategory.Pickupables;
            
            if (BuildingsChecksum != other.BuildingsChecksum)
                mismatched |= WorldSyncCategory.Buildings;
            
            if (DuplicantsChecksum != other.DuplicantsChecksum)
                mismatched |= WorldSyncCategory.Duplicants;
            
            if (ConduitsChecksum != other.ConduitsChecksum)
                mismatched |= WorldSyncCategory.Conduits;
            
            // Gases and liquids can have small variations due to sim timing
            // Only flag if significantly different
            if (GasesChecksum != other.GasesChecksum)
                mismatched |= WorldSyncCategory.Gases;
            
            if (LiquidsChecksum != other.LiquidsChecksum)
                mismatched |= WorldSyncCategory.Liquids;
            
            return mismatched;
        }
    }

    /// <summary>
    /// Data for partial resync of a category
    /// </summary>
    public class CategorySyncData
    {
        public WorldSyncCategory Category { get; set; }
        public int GameTick { get; set; }
        public byte[] Data { get; set; }
        public int ItemCount { get; set; }
    }
}
