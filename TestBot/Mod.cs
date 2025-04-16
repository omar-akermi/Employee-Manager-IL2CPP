using MelonLoader;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.Storage;
using Object = UnityEngine.Object;
using SharedModUtils;
using HarmonyLib;
using Il2CppScheduleOne.GameTime; // If available

[assembly: MelonInfo(typeof(TestBot.Mod), "Employee Manager", "1.6", "Akermi")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace TestBot
{
    public class Mod : MelonMod
    {
        private BotManager _botManager;

        private TimeManager _timeManager;
        private int _lastProcessedDay = -1;

        public override void OnInitializeMelon()
        {
            EmployeeConfigManager.LoadConfig();
            MelonLogger.Msg("[Employee_Manager] Config loaded.");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene loaded: {sceneName}");

            if (sceneName == "Main")
            {
                _timeManager = UnityEngine.Object.FindObjectOfType<TimeManager>();
                if (_timeManager != null)
                {
                    _lastProcessedDay = _timeManager.DayIndex;
                    MelonLogger.Msg($"[AutoPay] Initialized on day {_lastProcessedDay}");
                }
                MelonCoroutines.Start(Initialize());
                MelonCoroutines.Start(WaitForPropertyRestoreThenPatch());

            }
        }

        private IEnumerator Initialize()
        {
            yield return BotManager.WaitForSystems();

            LoggerInstance.Msg("[Employee_Manager] BotManager...");
            _botManager = BotManager.Create(this);
        }

        private IEnumerator WaitForPropertyRestoreThenPatch()
        {
            // Wait until Property list is initialized
            while (Property.Properties == null || Property.Properties.Count == 0)
                yield return null;

            // Wait until at least one property is owned or matches saved config
            bool hasTargetProps = false;
            while (!hasTargetProps)
            {
                foreach (var prop in Property.Properties)
                {
                    if ((prop.IsOwned || EmployeeConfigManager.HasCapacity(prop.PropertyName)) && prop.PropertyName != null)
                    {
                        hasTargetProps = true;
                        break;
                    }
                }

                if (!hasTargetProps)
                    yield return null;
            }

            foreach (var prop in Property.Properties)
            {
                if (string.IsNullOrWhiteSpace(prop.PropertyName))
                {
                    MelonLogger.Warning("[EmployeeManager] Skipping unnamed property.");
                    continue;
                }

                if (!prop.IsOwned && !EmployeeConfigManager.HasCapacity(prop.PropertyName))
                {
                    MelonLogger.Msg($"[EmployeeManager] Skipping {prop.PropertyName} — not owned and not saved.");
                    continue;
                }

                // Load saved or fallback capacity
                int capacity = EmployeeConfigManager.GetCapacity(prop.PropertyName, 10);
                prop.EmployeeCapacity = capacity;

                // Assign idle points
                prop.EmployeeIdlePoints = BotManagerStaticUtils.GetIdleTransformsForProperty(
                    prop.PropertyName,
                    prop.EmployeeCapacity
                );

                MelonLogger.Msg($"[✔] Patched {prop.PropertyName} → capacity: {prop.EmployeeCapacity}, idlePoints: {prop.EmployeeIdlePoints?.Length}");
            }
        }
        private static string NormalizeName(string name)
        {
            return name?.Replace(" ", "").Trim().ToLowerInvariant();
        }
        public override void OnUpdate()
        {
            _timeManager ??= UnityEngine.Object.FindObjectOfType<TimeManager>();
            if (_timeManager == null) return;

            int currentDay = _timeManager.DayIndex;

            if (currentDay != _lastProcessedDay)
            {
                _lastProcessedDay = currentDay;
                MelonLogger.Msg($"[AutoPay] New day {currentDay} detected");

                foreach (var prop in Il2CppScheduleOne.Property.Property.Properties)
                {
                    if (EmployeeConfigManager.IsAutoPaymentEnabled(prop.PropertyName))
                    {
                        float due = SharedModUtils.WageUtils.GetTotalUnpaidWagesForProperty(prop);
                        if (due > 0f && prop.Employees != null)
                        {
                            SharedModUtils.WageUtils.TryPayAllUnpaidEmployees(prop.Employees, out _, out _);
                            MelonLogger.Msg($"[AutoPay] Paid ${due} in {prop.PropertyName}");
                        }
                    }
                }
            }
        }



        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                foreach (var prop in Property.Properties)
                {
                    if (prop.IsOwned && prop.PropertyName != null)
                    {
                        EmployeeConfigManager.SetCapacity(prop.PropertyName, prop.EmployeeCapacity);
                        MelonLogger.Msg($"[💾] Saved {prop.PropertyName} = {prop.EmployeeCapacity}");
                    }
                }

                EmployeeConfigManager.SaveConfig();
                _botManager?.Cleanup();
                _botManager = null;
            }
        }
    }
}
