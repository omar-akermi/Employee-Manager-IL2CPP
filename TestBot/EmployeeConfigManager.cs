using Newtonsoft.Json;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections.Generic;
using System.IO;

namespace TestBot;

public static class EmployeeConfigManager
{
    public static Dictionary<string, int> PropertyCapacities = new();
    public static Dictionary<string, bool> AutoPaymentToggles = new();

    private static readonly string ConfigPath = Path.Combine(
        MelonEnvironment.UserDataDirectory,
        "Mods", "Configs", "employee_config.json"
    );

    private static string NormalizeName(string name)
    {
        return name?.Replace(" ", "").Trim().ToLowerInvariant();
    }

    public static string Normalize(string name) => NormalizeName(name);

    public static void ToggleAutoPayment(string propertyName)
    {
        string key = NormalizeName(propertyName);
        bool current = AutoPaymentToggles.ContainsKey(key) && AutoPaymentToggles[key];
        AutoPaymentToggles[key] = !current;
        SaveConfig();
    }

    public static bool IsAutoPaymentEnabled(string propertyName)
    {
        string key = NormalizeName(propertyName);
        return AutoPaymentToggles.ContainsKey(key) && AutoPaymentToggles[key];
    }

    public static void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                MelonLogger.Msg($"[EmployeeManager] No config found. Starting fresh. {ConfigPath}");
                return;
            }

            string json = File.ReadAllText(ConfigPath);

            // Try new format
            var root = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (root != null && root.ContainsKey("capacities") && root.ContainsKey("autoPay"))
            {
                PropertyCapacities = JsonConvert.DeserializeObject<Dictionary<string, int>>(root["capacities"].ToString());
                AutoPaymentToggles = JsonConvert.DeserializeObject<Dictionary<string, bool>>(root["autoPay"].ToString());
                MelonLogger.Msg($"[EmployeeManager] Loaded {PropertyCapacities.Count} capacities and {AutoPaymentToggles.Count} auto-pay toggles.");
            }
            else
            {
                // Fallback: legacy flat format (just capacities)
                var flat = JsonConvert.DeserializeObject<Dictionary<string, int>>(json)
                           ?? new Dictionary<string, int>();

                PropertyCapacities = new();
                foreach (var entry in flat)
                {
                    string key = NormalizeName(entry.Key);
                    if (!PropertyCapacities.ContainsKey(key))
                        PropertyCapacities[key] = entry.Value;
                }

                AutoPaymentToggles = new();
                SaveConfig(); // Convert to new format
                MelonLogger.Msg($"[EmployeeManager] Migrated old config with {PropertyCapacities.Count} capacities.");
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Msg($"[EmployeeManager] Failed to load config: {ex.Message}");
            PropertyCapacities = new();
            AutoPaymentToggles = new();
        }
    }

    public static void SaveConfig()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);

            var data = new Dictionary<string, object>
            {
                { "capacities", PropertyCapacities },
                { "autoPay", AutoPaymentToggles }
            };

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
            MelonLogger.Msg("[EmployeeManager] Config saved.");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[EmployeeManager] Failed to save config: {ex.Message}");
        }
    }

    public static int GetCapacity(string propertyName, int fallback = 10)
    {
        var key = NormalizeName(propertyName);
        return PropertyCapacities.TryGetValue(key, out var val) ? val : fallback;
    }

    public static void SetCapacity(string propertyName, int value)
    {
        var key = NormalizeName(propertyName);
        if (string.IsNullOrWhiteSpace(key))
        {
            MelonLogger.Msg("[EmployeeManager] Skipped saving capacity for unnamed property.");
            return;
        }

        PropertyCapacities[key] = value;
        MelonLogger.Msg($"[EmployeeManager] Saved capacity for {propertyName}: {value}");
    }

    public static bool HasCapacity(string propertyName)
    {
        var key = NormalizeName(propertyName);
        return !string.IsNullOrWhiteSpace(key) && PropertyCapacities.ContainsKey(key);
    }
}
