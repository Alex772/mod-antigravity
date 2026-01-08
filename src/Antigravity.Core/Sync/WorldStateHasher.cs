using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Antigravity.Core.Sync
{
    /// <summary>
    /// Calculates hashes/checksums for different categories of world state.
    /// Used to detect desynchronization between host and clients.
    /// </summary>
    public static class WorldStateHasher
    {
        private const int GAS_SAMPLE_COUNT = 100;
        private const int LIQUID_SAMPLE_COUNT = 100;
        
        /// <summary>
        /// Calculate checksums for all categories
        /// </summary>
        public static WorldStateChecksums CalculateAllChecksums()
        {
            var checksums = new WorldStateChecksums
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                GameTick = GameClock.Instance?.GetCycle() ?? 0
            };

            try
            {
                // Pickupables (items on ground)
                var pickupableData = HashPickupables();
                checksums.PickupablesChecksum = pickupableData.hash;
                checksums.PickupablesCount = pickupableData.count;

                // Buildings
                var buildingData = HashBuildings();
                checksums.BuildingsChecksum = buildingData.hash;
                checksums.BuildingsCount = buildingData.count;

                // Duplicants
                var dupeData = HashDuplicants();
                checksums.DuplicantsChecksum = dupeData.hash;
                checksums.DuplicantsCount = dupeData.count;

                // Conduits
                checksums.ConduitsChecksum = HashConduits();

                // Sampled gas/liquid state
                checksums.GasesChecksum = SampleGasState();
                checksums.LiquidsChecksum = SampleLiquidState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Error calculating checksums: {ex.Message}");
            }

            return checksums;
        }

        /// <summary>
        /// Hash all pickupable items (resources on the ground)
        /// </summary>
        public static (long hash, int count) HashPickupables()
        {
            long hash = 0;
            int count = 0;

            try
            {
                var pickupables = Components.Pickupables.Items;
                if (pickupables == null) return (0, 0);

                foreach (var pickupable in pickupables)
                {
                    if (pickupable == null || !pickupable.gameObject.activeInHierarchy) continue;

                    // Hash: position + element/prefab + amount
                    var pos = pickupable.transform.position;
                    int cell = Grid.PosToCell(pos);
                    
                    var element = pickupable.GetComponent<PrimaryElement>();
                    ushort elementIdx = element != null ? (ushort)element.ElementID : (ushort)0;
                    float mass = element?.Mass ?? 0;

                    // Combine into hash
                    hash ^= HashCombine(cell, elementIdx, (int)(mass * 100));
                    count++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Error hashing pickupables: {ex.Message}");
            }

            return (hash, count);
        }

        /// <summary>
        /// Hash all buildings
        /// </summary>
        public static (long hash, int count) HashBuildings()
        {
            long hash = 0;
            int count = 0;

            try
            {
                var buildings = Components.BuildingCompletes.Items;
                if (buildings == null) return (0, 0);

                foreach (var building in buildings)
                {
                    if (building == null) continue;

                    int cell = Grid.PosToCell(building.transform.position);
                    int prefabHash = building.Def?.PrefabID?.GetHashCode() ?? 0;
                    
                    // Include operational state
                    var operational = building.GetComponent<Operational>();
                    int opState = operational?.IsOperational == true ? 1 : 0;

                    hash ^= HashCombine(cell, prefabHash, opState);
                    count++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Error hashing buildings: {ex.Message}");
            }

            return (hash, count);
        }

        /// <summary>
        /// Hash duplicant states
        /// </summary>
        public static (long hash, int count) HashDuplicants()
        {
            long hash = 0;
            int count = 0;

            try
            {
                var minions = Components.MinionIdentities.Items;
                if (minions == null) return (0, 0);

                foreach (var minion in minions)
                {
                    if (minion == null) continue;

                    int cell = Grid.PosToCell(minion.transform.position);
                    int nameHash = minion.name?.GetHashCode() ?? 0;
                    
                    // Include health/stress roughly
                    var health = minion.GetComponent<Health>();
                    int healthPct = health != null ? (int)(health.hitPoints * 10) : 100;

                    hash ^= HashCombine(cell, nameHash, healthPct);
                    count++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Error hashing duplicants: {ex.Message}");
            }

            return (hash, count);
        }

        /// <summary>
        /// Hash conduit contents
        /// </summary>
        public static long HashConduits()
        {
            long hash = 0;

            try
            {
                // Gas conduits
                if (Game.Instance?.gasConduitSystem != null)
                {
                    hash ^= HashConduitNetwork(Game.Instance.gasConduitFlow);
                }

                // Liquid conduits
                if (Game.Instance?.liquidConduitSystem != null)
                {
                    hash ^= HashConduitNetwork(Game.Instance.liquidConduitFlow);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Error hashing conduits: {ex.Message}");
            }

            return hash;
        }

        private static long HashConduitNetwork(ConduitFlow flow)
        {
            if (flow == null) return 0;
            
            long hash = 0;
            // Sample some conduit cells
            int sampleInterval = Math.Max(1, Grid.CellCount / 1000);
            
            for (int cell = 0; cell < Grid.CellCount; cell += sampleInterval)
            {
                var contents = flow.GetContents(cell);
                if (contents.mass > 0)
                {
                    hash ^= HashCombine(cell, (int)contents.element, (int)(contents.mass * 100));
                }
            }
            
            return hash;
        }

        /// <summary>
        /// Sample gas state across the world
        /// </summary>
        public static long SampleGasState()
        {
            return SampleElementState(isGas: true);
        }

        /// <summary>
        /// Sample liquid state across the world
        /// </summary>
        public static long SampleLiquidState()
        {
            return SampleElementState(isGas: false);
        }

        private static long SampleElementState(bool isGas)
        {
            long hash = 0;

            try
            {
                if (Grid.CellCount == 0) return 0;

                // Sample random cells deterministically based on game tick
                int seed = GameClock.Instance?.GetCycle() ?? 0;
                var random = new System.Random(seed);
                int sampleCount = isGas ? GAS_SAMPLE_COUNT : LIQUID_SAMPLE_COUNT;

                for (int i = 0; i < sampleCount; i++)
                {
                    int cell = random.Next(Grid.CellCount);
                    if (!Grid.IsValidCell(cell)) continue;

                    Element element = Grid.Element[cell];
                    if (element == null) continue;

                    bool isTargetState = isGas ? element.IsGas : element.IsLiquid;
                    if (!isTargetState) continue;

                    float mass = Grid.Mass[cell];
                    int temp = (int)Grid.Temperature[cell];

                    hash ^= HashCombine(cell, (int)element.idx, (int)(mass * 10), temp / 10);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Error sampling {(isGas ? "gas" : "liquid")} state: {ex.Message}");
            }

            return hash;
        }

        /// <summary>
        /// Combine multiple values into a hash
        /// </summary>
        private static long HashCombine(params int[] values)
        {
            long hash = 17;
            foreach (var value in values)
            {
                hash = hash * 31 + value;
            }
            return hash;
        }

        #region Serialization for Partial Sync

        /// <summary>
        /// Serialize pickupables for resync
        /// </summary>
        public static byte[] SerializePickupables()
        {
            var pickupableList = new List<PickupableData>();

            try
            {
                var pickupables = Components.Pickupables.Items;
                if (pickupables == null) return Array.Empty<byte>();

                foreach (var pickupable in pickupables)
                {
                    if (pickupable == null || !pickupable.gameObject.activeInHierarchy) continue;

                    var element = pickupable.GetComponent<PrimaryElement>();
                    if (element == null) continue;

                    pickupableList.Add(new PickupableData
                    {
                        Cell = Grid.PosToCell(pickupable.transform.position),
                        ElementId = (ushort)element.ElementID,
                        Mass = element.Mass,
                        Temperature = element.Temperature,
                        PrefabTag = pickupable.PrefabID().ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Error serializing pickupables: {ex.Message}");
            }

            string json = JsonConvert.SerializeObject(pickupableList);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Serialize buildings for resync
        /// </summary>
        public static byte[] SerializeBuildings()
        {
            var buildingList = new List<BuildingData>();

            try
            {
                var buildings = Components.BuildingCompletes.Items;
                if (buildings == null) return Array.Empty<byte>();

                foreach (var building in buildings)
                {
                    if (building == null) continue;

                    var operational = building.GetComponent<Operational>();
                    
                    buildingList.Add(new BuildingData
                    {
                        Cell = Grid.PosToCell(building.transform.position),
                        PrefabId = building.Def?.PrefabID?.ToString() ?? "",
                        IsOperational = operational?.IsOperational ?? false,
                        Orientation = (int)building.Orientation
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Error serializing buildings: {ex.Message}");
            }

            string json = JsonConvert.SerializeObject(buildingList);
            return Encoding.UTF8.GetBytes(json);
        }

        #endregion

        #region Data Classes for Serialization

        [Serializable]
        public class PickupableData
        {
            public int Cell;
            public ushort ElementId;
            public float Mass;
            public float Temperature;
            public string PrefabTag;
        }

        [Serializable]
        public class BuildingData
        {
            public int Cell;
            public string PrefabId;
            public bool IsOperational;
            public int Orientation;
        }

        [Serializable]
        public class ElementData
        {
            public int Cell;
            public int ElementId;
            public float Mass;
            public float Temperature;
        }

        #endregion

        #region Element Serialization for Partial Sync

        /// <summary>
        /// Serialize gas elements in the world for resync
        /// </summary>
        public static byte[] SerializeGasRegion()
        {
            return SerializeElementRegion(isGas: true);
        }

        /// <summary>
        /// Serialize liquid elements in the world for resync
        /// </summary>
        public static byte[] SerializeLiquidRegion()
        {
            return SerializeElementRegion(isGas: false);
        }

        /// <summary>
        /// Serialize elements of a specific type (gas or liquid)
        /// Only serializes cells with significant mass to reduce data size
        /// </summary>
        private static byte[] SerializeElementRegion(bool isGas)
        {
            var elementList = new List<ElementData>();

            try
            {
                if (Grid.CellCount == 0) return Array.Empty<byte>();

                int cellCount = Grid.CellCount;
                float minMass = isGas ? 0.01f : 0.1f; // Minimum mass to consider

                for (int cell = 0; cell < cellCount; cell++)
                {
                    if (!Grid.IsValidCell(cell)) continue;

                    Element element = Grid.Element[cell];
                    if (element == null) continue;

                    bool isTargetType = isGas ? element.IsGas : element.IsLiquid;
                    if (!isTargetType) continue;

                    float mass = Grid.Mass[cell];
                    if (mass < minMass) continue;

                    elementList.Add(new ElementData
                    {
                        Cell = cell,
                        ElementId = (int)element.id,
                        Mass = mass,
                        Temperature = Grid.Temperature[cell]
                    });
                }

                Debug.Log($"[Antigravity] Serialized {elementList.Count} {(isGas ? "gas" : "liquid")} cells");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Error serializing {(isGas ? "gas" : "liquid")} region: {ex.Message}");
            }

            string json = JsonConvert.SerializeObject(elementList);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Deserialize and apply element data from host
        /// </summary>
        public static void ApplyElementData(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            try
            {
                string json = Encoding.UTF8.GetString(data);
                var elementList = JsonConvert.DeserializeObject<List<ElementData>>(json);
                
                if (elementList == null || elementList.Count == 0) return;

                int applied = 0;
                foreach (var elem in elementList)
                {
                    if (!Grid.IsValidCell(elem.Cell)) continue;

                    SimMessages.ReplaceElement(
                        gameCell: elem.Cell,
                        new_element: (SimHashes)elem.ElementId,
                        ev: null,
                        mass: elem.Mass,
                        temperature: elem.Temperature,
                        diseaseIdx: byte.MaxValue,
                        diseaseCount: 0
                    );
                    applied++;
                }

                Debug.Log($"[Antigravity] Applied {applied} element changes from resync");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity] Error applying element data: {ex.Message}");
            }
        }

        #endregion
    }
}
