using HarmonyLib;
using Database;

namespace Antigravity.Patches
{
    /// <summary>
    /// Manages all Harmony patches for the mod.
    /// </summary>
    public static class PatchManager
    {
        /// <summary>
        /// Whether patches have been applied.
        /// </summary>
        public static bool PatchesApplied { get; private set; }

        /// <summary>
        /// Apply all patches.
        /// </summary>
        /// <param name="harmony">Harmony instance to use.</param>
        public static void ApplyPatches(Harmony harmony)
        {
            if (PatchesApplied)
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Patches already applied!");
                return;
            }

            try
            {
                // Apply UI patches
                ApplyUIPatches(harmony);

                // Apply game patches
                ApplyGamePatches(harmony);

                // Apply simulation patches
                ApplySimulationPatches(harmony);

                PatchesApplied = true;
                UnityEngine.Debug.Log("[Antigravity] All patches applied successfully.");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[Antigravity] Failed to apply patches: {ex.Message}");
                UnityEngine.Debug.LogError(ex.StackTrace);
            }
        }

        private static void ApplyUIPatches(Harmony harmony)
        {
            // Main menu patch - adds Multiplayer button
            harmony.Patch(
                AccessTools.Method(typeof(MainMenu), "OnSpawn"),
                postfix: new HarmonyMethod(typeof(UI.MainMenuPatch), "Postfix")
            );
            UnityEngine.Debug.Log("[Antigravity] UI patches applied.");
        }

        private static void ApplyGamePatches(Harmony harmony)
        {
            // Patch SaveLoader.Load to detect when a save is loaded
            var saveLoaderLoadMethod = AccessTools.Method(typeof(SaveLoader), "Load", new System.Type[] { typeof(string) });
            if (saveLoaderLoadMethod != null)
            {
                harmony.Patch(
                    saveLoaderLoadMethod,
                    postfix: new HarmonyMethod(typeof(Game.SaveLoader_Load_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] SaveLoader.Load patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] SaveLoader.Load method not found - save sync may not work.");
            }

            // Patch Game.OnSpawn to detect when world is ready
            var gameOnSpawnMethod = AccessTools.Method(typeof(global::Game), "OnSpawn");
            if (gameOnSpawnMethod != null)
            {
                harmony.Patch(
                    gameOnSpawnMethod,
                    postfix: new HarmonyMethod(typeof(Game.Game_OnSpawn_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] Game.OnSpawn patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Game.OnSpawn method not found.");
            }
            
            UnityEngine.Debug.Log("[Antigravity] Game patches applied.");

            // Apply sync patches
            ApplySpeedSyncPatches(harmony);
            ApplyDigSyncPatches(harmony);
            ApplyCancelSyncPatches(harmony);
            ApplyBuildSyncPatches(harmony);
            ApplyUtilityBuildSyncPatches(harmony);
            ApplyDeconstructSyncPatches(harmony);
            ApplyMopSyncPatches(harmony);
            ApplyClearSyncPatches(harmony);
            ApplyHarvestSyncPatches(harmony);
            ApplyDisinfectSyncPatches(harmony);
            ApplyCaptureSyncPatches(harmony);
            ApplyPrioritizeSyncPatches(harmony);
            ApplyDoorSyncPatches(harmony);
            // ApplyBuildingSettingsSyncPatches(harmony); // DISABLED TO TEST CRASH
            
            // TEMPORARILY DISABLED FOR DEBUGGING - uncomment after testing
            // ApplyHardSyncPatches(harmony);
            // ApplyResearchPatches(harmony);
            // ApplySkillsPatches(harmony);
            // ApplySchedulePatches(harmony);
        }

        private static void ApplySpeedSyncPatches(Harmony harmony)
        {
            // Try to find Pause method - may have different signature
            var pauseMethod = AccessTools.Method(typeof(SpeedControlScreen), "Pause", new System.Type[] { typeof(bool) });
            if (pauseMethod == null)
            {
                // Try without parameters
                pauseMethod = AccessTools.Method(typeof(SpeedControlScreen), "Pause");
                UnityEngine.Debug.Log("[Antigravity] Trying Pause() without parameters...");
            }
            
            if (pauseMethod != null)
            {
                harmony.Patch(
                    pauseMethod,
                    postfix: new HarmonyMethod(typeof(Commands.SpeedSyncPatches.SpeedControlScreen_Pause_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] SpeedControlScreen.Pause patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] SpeedControlScreen.Pause method NOT FOUND! Pause sync will not work.");
            }

            // Patch SpeedControlScreen.Unpause
            var unpauseMethod = AccessTools.Method(typeof(SpeedControlScreen), "Unpause", new System.Type[] { typeof(bool) });
            if (unpauseMethod == null)
            {
                unpauseMethod = AccessTools.Method(typeof(SpeedControlScreen), "Unpause");
            }
            
            if (unpauseMethod != null)
            {
                harmony.Patch(
                    unpauseMethod,
                    postfix: new HarmonyMethod(typeof(Commands.SpeedSyncPatches.SpeedControlScreen_Unpause_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] SpeedControlScreen.Unpause patch applied.");
            }

            // Patch SpeedControlScreen.SetSpeed
            var setSpeedMethod = AccessTools.Method(typeof(SpeedControlScreen), "SetSpeed", new System.Type[] { typeof(int) });
            if (setSpeedMethod != null)
            {
                harmony.Patch(
                    setSpeedMethod,
                    postfix: new HarmonyMethod(typeof(Commands.SpeedSyncPatches.SpeedControlScreen_SetSpeed_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] SpeedControlScreen.SetSpeed patch applied.");
            }

            UnityEngine.Debug.Log("[Antigravity] Speed sync patches applied.");
        }

        private static void ApplyDigSyncPatches(Harmony harmony)
        {
            // Patch DigTool.OnDragTool
            var onDragToolMethod = AccessTools.Method(typeof(DigTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.DigSyncPatches.DigTool_OnDragTool_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] DigTool.OnDragTool patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] DigTool.OnDragTool method NOT FOUND!");
            }

            // Patch DragTool.OnLeftClickUp to detect when drag ends
            var onLeftClickUpMethod = AccessTools.Method(typeof(DragTool), "OnLeftClickUp");
            if (onLeftClickUpMethod != null)
            {
                harmony.Patch(
                    onLeftClickUpMethod,
                    postfix: new HarmonyMethod(typeof(Commands.DigSyncPatches.DragTool_OnLeftClickUp_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] DragTool.OnLeftClickUp patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] DragTool.OnLeftClickUp method NOT FOUND!");
            }

            UnityEngine.Debug.Log("[Antigravity] Dig sync patches applied.");
        }

        private static void ApplyCancelSyncPatches(Harmony harmony)
        {
            // Patch CancelTool.OnDragTool  
            var onDragToolMethod = AccessTools.Method(typeof(CancelTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.CancelSyncPatches.CancelTool_OnDragTool_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] CancelTool.OnDragTool patch applied.");
            }

            // Note: OnLeftClickUp is patched via DragTool already for Dig
            // The CancelTool postfix handler checks if instance is CancelTool
            UnityEngine.Debug.Log("[Antigravity] Cancel sync patches applied.");
        }

        private static void ApplyBuildSyncPatches(Harmony harmony)
        {
            // Patch BuildTool.OnDragTool
            var onDragToolMethod = AccessTools.Method(typeof(BuildTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.BuildSyncPatches.BuildTool_OnDragTool_Patch), "Postfix")
                );
            }

            // Patch DragTool.OnLeftClickUp for build (already patched, but will call BuildTool handler)
            UnityEngine.Debug.Log("[Antigravity] Build sync patches applied.");
        }

        private static void ApplyUtilityBuildSyncPatches(Harmony harmony)
        {
            // Patch BaseUtilityBuildTool.BuildPath for wires, pipes, logic wires
            var buildPathMethod = AccessTools.Method(typeof(BaseUtilityBuildTool), "BuildPath");
            if (buildPathMethod != null)
            {
                harmony.Patch(
                    buildPathMethod,
                    prefix: new HarmonyMethod(typeof(Commands.UtilityBuildSyncPatches.BaseUtilityBuildTool_BuildPath_Patch), "Prefix")
                );
                UnityEngine.Debug.Log("[Antigravity] BaseUtilityBuildTool.BuildPath patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] BaseUtilityBuildTool.BuildPath method NOT FOUND!");
            }

            UnityEngine.Debug.Log("[Antigravity] Utility build sync patches applied.");
        }

        private static void ApplyDeconstructSyncPatches(Harmony harmony)
        {
            // Patch DeconstructTool.OnDragTool
            var onDragToolMethod = AccessTools.Method(typeof(DeconstructTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.DeconstructSyncPatches.DeconstructTool_OnDragTool_Patch), "Postfix")
                );
            }

            UnityEngine.Debug.Log("[Antigravity] Deconstruct sync patches applied.");
        }

        private static void ApplySimulationPatches(Harmony harmony)
        {
            // Simulation patches will be applied here
            // harmony.PatchAll(typeof(SimTickPatch));
        }

        private static void ApplyMopSyncPatches(Harmony harmony)
        {
            var onDragToolMethod = AccessTools.Method(typeof(MopTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.MopSyncPatches.MopTool_OnDragTool_Patch), "Postfix")
                );
            }
            UnityEngine.Debug.Log("[Antigravity] Mop sync patches applied.");
        }

        private static void ApplyClearSyncPatches(Harmony harmony)
        {
            var onDragToolMethod = AccessTools.Method(typeof(ClearTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.ClearSyncPatches.ClearTool_OnDragTool_Patch), "Postfix")
                );
            }
            UnityEngine.Debug.Log("[Antigravity] Clear sync patches applied.");
        }

        private static void ApplyHarvestSyncPatches(Harmony harmony)
        {
            var onDragToolMethod = AccessTools.Method(typeof(HarvestTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.HarvestSyncPatches.HarvestTool_OnDragTool_Patch), "Postfix")
                );
            }
            UnityEngine.Debug.Log("[Antigravity] Harvest sync patches applied.");
        }

        private static void ApplyDisinfectSyncPatches(Harmony harmony)
        {
            var onDragToolMethod = AccessTools.Method(typeof(DisinfectTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.DisinfectSyncPatches.DisinfectTool_OnDragTool_Patch), "Postfix")
                );
            }
            UnityEngine.Debug.Log("[Antigravity] Disinfect sync patches applied.");
        }

        private static void ApplyCaptureSyncPatches(Harmony harmony)
        {
            // CaptureTool uses OnDragComplete, not OnDragTool
            var onDragCompleteMethod = AccessTools.Method(typeof(CaptureTool), "OnDragComplete", new System.Type[] { typeof(UnityEngine.Vector3), typeof(UnityEngine.Vector3) });
            if (onDragCompleteMethod != null)
            {
                harmony.Patch(
                    onDragCompleteMethod,
                    postfix: new HarmonyMethod(typeof(Commands.CaptureSyncPatches.CaptureTool_OnDragComplete_Patch), "Postfix")
                );
            }
            UnityEngine.Debug.Log("[Antigravity] Capture sync patches applied.");
        }

        private static void ApplyPrioritizeSyncPatches(Harmony harmony)
        {
            // PrioritizeTool inherits from FilteredDragTool
            var onDragToolMethod = AccessTools.Method(typeof(PrioritizeTool), "OnDragTool", new System.Type[] { typeof(int), typeof(int) });
            if (onDragToolMethod != null)
            {
                harmony.Patch(
                    onDragToolMethod,
                    postfix: new HarmonyMethod(typeof(Commands.PrioritizeSyncPatches.PrioritizeTool_OnDragTool_Patch), "Postfix")
                );
            }
            UnityEngine.Debug.Log("[Antigravity] Prioritize sync patches applied.");
        }

        private static void ApplyDoorSyncPatches(Harmony harmony)
        {
            // Patch Door.QueueStateChange
            var queueStateChangeMethod = AccessTools.Method(typeof(Door), "QueueStateChange", new System.Type[] { typeof(Door.ControlState) });
            if (queueStateChangeMethod != null)
            {
                harmony.Patch(
                    queueStateChangeMethod,
                    postfix: new HarmonyMethod(typeof(Commands.DoorSyncPatches.Door_QueueStateChange_Patch), "Postfix")
                );
            }
            UnityEngine.Debug.Log("[Antigravity] Door sync patches applied.");
        }

        private static void ApplyBuildingSettingsSyncPatches(Harmony harmony)
        {
            // Patch Prioritizable.SetMasterPriority - for priority changes via UI sidebar
            var setMasterPriorityMethod = AccessTools.Method(typeof(Prioritizable), "SetMasterPriority", new System.Type[] { typeof(PrioritySetting) });
            if (setMasterPriorityMethod != null)
            {
                harmony.Patch(
                    setMasterPriorityMethod,
                    postfix: new HarmonyMethod(typeof(Commands.BuildingSettingsSyncPatches.Prioritizable_SetMasterPriority_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] Prioritizable.SetMasterPriority patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Prioritizable.SetMasterPriority method NOT FOUND!");
            }

            // Patch Disinfectable.MarkForDisinfect - for disinfect via UI
            var markForDisinfectMethod = AccessTools.Method(typeof(Disinfectable), "MarkForDisinfect", new System.Type[] { typeof(bool) });
            if (markForDisinfectMethod != null)
            {
                harmony.Patch(
                    markForDisinfectMethod,
                    postfix: new HarmonyMethod(typeof(Commands.BuildingSettingsSyncPatches.Disinfectable_MarkForDisinfect_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] Disinfectable.MarkForDisinfect patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Disinfectable.MarkForDisinfect method NOT FOUND!");
            }

            // Patch Disinfectable cancel - OnCancel is private, we need to patch CancelDisinfection
            var cancelDisinfectionMethod = AccessTools.Method(typeof(Disinfectable), "CancelDisinfection");
            if (cancelDisinfectionMethod != null)
            {
                harmony.Patch(
                    cancelDisinfectionMethod,
                    postfix: new HarmonyMethod(typeof(Commands.BuildingSettingsSyncPatches.Disinfectable_OnCancel_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] Disinfectable.CancelDisinfection patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Disinfectable.CancelDisinfection method NOT FOUND!");
            }

            // Patch TreeFilterable.UpdateFilters - for storage filter changes via UI
            var updateFiltersMethod = AccessTools.Method(typeof(TreeFilterable), "UpdateFilters", new System.Type[] { typeof(System.Collections.Generic.HashSet<Tag>) });
            if (updateFiltersMethod != null)
            {
                harmony.Patch(
                    updateFiltersMethod,
                    postfix: new HarmonyMethod(typeof(Commands.BuildingSettingsSyncPatches.TreeFilterable_UpdateFilters_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] TreeFilterable.UpdateFilters patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] TreeFilterable.UpdateFilters method NOT FOUND!");
            }

            // Patch Filterable.SelectedTag setter - for element filter changes (valves, pumps)
            var filterableTagProperty = AccessTools.PropertySetter(typeof(Filterable), "SelectedTag");
            if (filterableTagProperty != null)
            {
                harmony.Patch(
                    filterableTagProperty,
                    postfix: new HarmonyMethod(typeof(Commands.BuildingSettingsSyncPatches.Filterable_SelectedTag_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] Filterable.SelectedTag patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Filterable.SelectedTag setter NOT FOUND!");
            }

            // LogicSwitch patch DISABLED - causes Harmony error because SetState is a virtual method
            // inherited from Switch base class. To fix this, we would need to patch Switch.SetState instead,
            // but that would affect all switches, not just LogicSwitch.
            // var logicSwitchToggleMethod = AccessTools.Method(typeof(LogicSwitch), "OnLogicValueChanged");
            // ... patch code removed to prevent crash ...

            // Patch common threshold switches (sensors)
            // Only patch LogicTemperatureSensor and LogicPressureSensor which definitely exist
            var thresholdTypes = new System.Type[] { 
                typeof(LogicTemperatureSensor), 
                typeof(LogicPressureSensor)
            };
            
            foreach (var sensorType in thresholdTypes)
            {
                try
                {
                    var thresholdSetter = AccessTools.PropertySetter(sensorType, "Threshold");
                    if (thresholdSetter != null)
                    {
                        harmony.Patch(
                            thresholdSetter,
                            postfix: new HarmonyMethod(typeof(Commands.BuildingSettingsSyncPatches.ThresholdSwitch_SetThreshold_Patch), "Postfix")
                        );
                        UnityEngine.Debug.Log($"[Antigravity] {sensorType.Name}.Threshold patch applied.");
                    }
                }
                catch { }
            }

            UnityEngine.Debug.Log("[Antigravity] Building settings sync patches applied.");
        }

        private static void ApplyHardSyncPatches(Harmony harmony)
        {
            // Patch GameClock for new day detection
            // GameClock fires an event when a new day starts
            var onNewDayMethod = AccessTools.Method(typeof(GameClock), "OnNewDay");
            if (onNewDayMethod != null)
            {
                harmony.Patch(
                    onNewDayMethod,
                    postfix: new HarmonyMethod(typeof(Sync.HardSyncPatches.GameClock_OnNewDay_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] GameClock.OnNewDay patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] GameClock.OnNewDay method NOT FOUND!");
            }

            // Patch SaveLoader.Save for manual save detection
            var saveMethod = AccessTools.Method(typeof(SaveLoader), "Save", new System.Type[] { typeof(string), typeof(bool), typeof(bool) });
            if (saveMethod != null)
            {
                harmony.Patch(
                    saveMethod,
                    postfix: new HarmonyMethod(typeof(Sync.HardSyncPatches.SaveLoader_Save_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] SaveLoader.Save patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] SaveLoader.Save method NOT FOUND!");
            }

            UnityEngine.Debug.Log("[Antigravity] Hard sync patches applied.");
        }

        private static void ApplyResearchPatches(Harmony harmony)
        {
            // Patch Research.SetActiveResearch - when player selects a tech
            var setActiveMethod = AccessTools.Method(typeof(Research), "SetActiveResearch", new System.Type[] { typeof(Tech), typeof(bool) });
            if (setActiveMethod != null)
            {
                harmony.Patch(
                    setActiveMethod,
                    postfix: new HarmonyMethod(typeof(Sync.ResearchPatches.Research_SetActiveResearch_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] Research.SetActiveResearch patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Research.SetActiveResearch method NOT FOUND!");
            }

            // Patch TechInstance.Purchased - when a tech is completed
            var techPurchasedMethod = AccessTools.Method(typeof(TechInstance), "Purchased");
            if (techPurchasedMethod != null)
            {
                harmony.Patch(
                    techPurchasedMethod,
                    postfix: new HarmonyMethod(typeof(Sync.ResearchPatches.TechInstance_Complete_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] TechInstance.Purchased patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] TechInstance.Purchased method NOT FOUND!");
            }

            // Patch Research.CancelResearch - when research is cancelled
            var cancelMethod = AccessTools.Method(typeof(Research), "CancelResearch");
            if (cancelMethod != null)
            {
                harmony.Patch(
                    cancelMethod,
                    postfix: new HarmonyMethod(typeof(Sync.ResearchPatches.Research_CancelResearch_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] Research.CancelResearch patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] Research.CancelResearch method NOT FOUND!");
            }

            UnityEngine.Debug.Log("[Antigravity] Research patches applied.");
        }

        private static void ApplySkillsPatches(Harmony harmony)
        {
            // Patch MinionResume.MasterSkill - when player assigns a skill
            var masterSkillMethod = AccessTools.Method(typeof(MinionResume), "MasterSkill", new System.Type[] { typeof(Skill) });
            if (masterSkillMethod != null)
            {
                harmony.Patch(
                    masterSkillMethod,
                    postfix: new HarmonyMethod(typeof(Sync.SkillsPatches.MinionResume_MasterSkill_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] MinionResume.MasterSkill patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] MinionResume.MasterSkill method NOT FOUND!");
            }

            // Patch MinionResume.UnmasterSkill - when player removes a skill
            var unmasterSkillMethod = AccessTools.Method(typeof(MinionResume), "UnmasterSkill", new System.Type[] { typeof(Skill) });
            if (unmasterSkillMethod != null)
            {
                harmony.Patch(
                    unmasterSkillMethod,
                    postfix: new HarmonyMethod(typeof(Sync.SkillsPatches.MinionResume_UnmasterSkill_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] MinionResume.UnmasterSkill patch applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Antigravity] MinionResume.UnmasterSkill method NOT FOUND!");
            }

            UnityEngine.Debug.Log("[Antigravity] Skills patches applied.");
        }

        private static void ApplySchedulePatches(Harmony harmony)
        {
            // Patch Schedule.SetBlocksToGroupDefaults - when player changes a block
            var setBlockMethod = AccessTools.Method(typeof(Schedule), "SetBlocksToGroupDefaults");
            if (setBlockMethod != null)
            {
                harmony.Patch(
                    setBlockMethod,
                    postfix: new HarmonyMethod(typeof(Sync.SchedulePatches.Schedule_SetBlockType_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] Schedule.SetBlocksToGroupDefaults patch applied.");
            }

            // Patch ScheduleManager.AddSchedule - when new schedule is created
            var addScheduleMethod = AccessTools.Method(typeof(ScheduleManager), "AddSchedule");
            if (addScheduleMethod != null)
            {
                harmony.Patch(
                    addScheduleMethod,
                    postfix: new HarmonyMethod(typeof(Sync.SchedulePatches.ScheduleManager_AddSchedule_Patch), "Postfix")
                );
                UnityEngine.Debug.Log("[Antigravity] ScheduleManager.AddSchedule patch applied.");
            }

            // Patch ScheduleManager.DeleteSchedule - when schedule is deleted
            var deleteScheduleMethod = AccessTools.Method(typeof(ScheduleManager), "DeleteSchedule");
            if (deleteScheduleMethod != null)
            {
                harmony.Patch(
                    deleteScheduleMethod,
                    prefix: new HarmonyMethod(typeof(Sync.SchedulePatches.ScheduleManager_DeleteSchedule_Patch), "Prefix")
                );
                UnityEngine.Debug.Log("[Antigravity] ScheduleManager.DeleteSchedule patch applied.");
            }

            UnityEngine.Debug.Log("[Antigravity] Schedule patches applied.");
        }

        /// <summary>
        /// Remove all patches.
        /// </summary>
        /// <param name="harmony">Harmony instance to use.</param>
        public static void RemovePatches(Harmony harmony)
        {
            harmony.UnpatchAll(harmony.Id);
            PatchesApplied = false;
            UnityEngine.Debug.Log("[Antigravity] All patches removed.");
        }
    }
}
