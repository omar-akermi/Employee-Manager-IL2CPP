using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone.ContactsApp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Money;
using Unity.Jobs.LowLevel.Unsafe;
using SharedModUtils;
namespace TestBot
{
    public class BotManager
    {
        private TestDialogue _dialogue;
        private GameObject _npcObject;
        private NPC _npc;

        public static IEnumerator WaitForSystems()
        {
            yield return new WaitForSeconds(3f);
        }
        private Mod _mod; // store reference

        public static BotManager Create(Mod mod)
        {
            var manager = new BotManager();
            manager.Init();
            manager._mod = mod;

            return manager;
        }

        private void Init()
        {

            NPC template = null;
            var allNpcs = NPCManager.NPCRegistry;

            for (int i = 0; i < allNpcs.Count; i++)
            {
                var npc = allNpcs[i];
                if (npc != null && npc.FirstName == "Beth")
                {
                    template = npc;
                    break;
                }
            }

            if (template == null)
            {
                return;
            }


            _npcObject = Object.Instantiate(template.gameObject);
            _npc = _npcObject.GetComponent<NPC>();

            _npc.FirstName = "Employee";
            _npc.LastName = "Manager";
            _npc.BakedGUID = System.Guid.NewGuid().ToString();
            _npc.IsImportant = true;
            _npc.ConversationCanBeHidden = false;
            _npc.ShowRelationshipInfo = false;
            _npc.MSGConversation = null;
            _npc.ConversationCategories = new Il2CppSystem.Collections.Generic.List<EConversationCategory>();
            _npc.dialogueHandler = null;
            _npc.MugshotSprite = PlayerSingleton<ContactsApp>.Instance.AppIcon; 


            _npcObject.SetActive(true);
            _npc.gameObject.SetActive(true);

            _npc.Awake();
            _npc.NetworkInitializeIfDisabled();
            NPCManager.NPCRegistry.Add(_npc);
            _npc.InitializeSaveable();


            _dialogue = new TestDialogue(_npc);
            Run();
        }
        private void Run()
        {
            _dialogue.SendNPCMessage("Hello!");
            _dialogue.SendNPCMessage("I'm here to help you manage your employees");

            _dialogue.ShowResponses(new DialogueResponse[]
            {
        new DialogueResponse("Manage properties", "Manage properties", ShowOwnedPropertiesMenu),
        new DialogueResponse("Pay employees", "Pay employees", ShowPayOptions)}, 0.5f);
        }
        private Transform[] GetIdleTransformsForProperty(string propertyName, int count)
        {
            var transforms = new Transform[count];
            var normalized = propertyName.Replace(" ", "").ToLowerInvariant();

            // 🔍 Find the matching Property object
            Property matchingProp = null;
            foreach (var prop in Il2CppScheduleOne.Property.Property.Properties)
            {
                if (prop.PropertyName == null) continue;
                if (prop.PropertyName.Replace(" ", "").ToLowerInvariant() == normalized)
                {
                    matchingProp = prop;
                    break;
                }
            }

            // 🛑 Fallback if property not found
            if (matchingProp == null || matchingProp.SpawnPoint == null)
            {
                MelonLoader.MelonLogger.Warning($"[EmployeeManager] Couldn't find spawn point for property: {propertyName}");
                return transforms;
            }

            // 🧱 Use spawn point position as base
            Vector3 basePos = matchingProp.SpawnPoint.position;
            float spacing = 1.2f;
            int columns = 5;

            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                float x = basePos.x + col * spacing;
                float z = basePos.z + row * spacing;
                float y = basePos.y;

                var idleGO = new GameObject($"IdlePoint_{i}_{propertyName}");
                idleGO.transform.position = new Vector3(x, y, z);

                transforms[i] = idleGO.transform;
            }

            return transforms;
        }
        private void ShowOwnedPropertiesMenu()
        {
            var responses = new System.Collections.Generic.List<DialogueResponse>();
            var allProps = Property.Properties;

            foreach (var prop in allProps)
            {
                                if (!prop.IsOwned || prop.PropertyName == null)
                    continue;

                // 🔁 Load from per-property config
                int savedCap = EmployeeConfigManager.GetCapacity(prop.PropertyName, 10);

                if (prop.EmployeeCapacity < savedCap)
                {
                    prop.EmployeeCapacity = savedCap;
                }

                // 🔁 Always assign idle points based on current capacity
                prop.EmployeeIdlePoints = GetIdleTransformsForProperty(prop.PropertyName, prop.EmployeeCapacity);

                responses.Add(new DialogueResponse(
                    $"{prop.PropertyName} (Cap: {prop.EmployeeCapacity})",
                    $"{prop.PropertyName} (Cap: {prop.EmployeeCapacity})",
                    () => ShowPropertyActions(prop),
                    true
                ));
            }

            if (responses.Count == 0)
            {
                _dialogue.SendNPCMessage("You don't own any properties yet.");
                return;
            }

            responses.Add(new DialogueResponse("BACK", "Back", Run, true));
            _dialogue.ShowResponses(responses.ToArray(), 0.5f);
        }
        private const int EmployeesPerPage = 6;

        private void ShowPropertyEmployeesWithBeds(Property prop, int page = 0)
        {
            var employeeList = prop.Employees;

            if (employeeList == null || employeeList.Count == 0)
            {
                _dialogue.SendNPCMessage($"No employees found in {prop.PropertyName}.");
                return;
            }

            var responses = new System.Collections.Generic.List<DialogueResponse>();
            int totalEmployees = employeeList.Count;
            int totalPages = Mathf.CeilToInt((float)totalEmployees / EmployeesPerPage);

            int start = page * EmployeesPerPage;
            int end = Mathf.Min(start + EmployeesPerPage, totalEmployees);

            for (int i = start; i < end; i++)
            {
                var emp = employeeList[i];
                if (emp == null) continue;

                var bed = emp.GetBed();
                string bedInfo = bed != null ? $"→ Bed at {bed.transform.position}" : "→ No bed assigned";

                responses.Add(new DialogueResponse(
                    $"{emp.FirstName} {emp.LastName} ({emp.name})",
                    $"{emp.FirstName} {emp.LastName} {bedInfo}",
                    () => OpenEmployeeBed(emp),
                    true
                ));
            }

            // Navigation
            if (page > 0)
            {
                responses.Add(new DialogueResponse("← Previous", "Previous Page", () => ShowPropertyEmployeesWithBeds(prop, page - 1), true));
            }

            if (page < totalPages - 1)
            {
                responses.Add(new DialogueResponse("→ Next", "Next Page", () => ShowPropertyEmployeesWithBeds(prop, page + 1), true));
            }

            // Back to property
            responses.Add(new DialogueResponse("↩ Back", "Back", () => ShowPropertyActions(prop), true));

            _dialogue.SendNPCMessage($"Showing employees in {prop.PropertyName} (Page {page + 1} of {totalPages})");
            _dialogue.ShowResponses(responses.ToArray(), 0.5f);
        }

        private void OpenEmployeeBed(Employee employee)
        {
            var bed = employee.GetBed();
            if (bed == null)
            {
                _dialogue.SendNPCMessage($"{employee.FirstName} has no assigned bed.");
                return;
            }

            var storage = bed.StorageEntity;
            if (storage == null)
            {
                _dialogue.SendNPCMessage($"Bed found, but not a valid storage.");
                return;
            }
            var gameplayMenu = Object.FindObjectOfType<GameplayMenu>();
            if (gameplayMenu != null)
            {
                gameplayMenu.SetIsOpen(false);

            }


            _dialogue.SendPlayerMessage($"Opened bed storage for {employee.FirstName}");
            // ✅ Open the storage
            StorageMenu.Instance.Open(storage);


            // ✅ Close the gameplay menu (like dealer inventory behavior)
           
        }
        private void ShowPayOptions()
        {
            var responses = new List<DialogueResponse>
    {
        new DialogueResponse("Pay all employees", "Pay all", () =>
        {
            var employeeManager = Object.FindObjectOfType<EmployeeManager>();
            float totalWages = WageUtils.GetTotalUnpaidWages(employeeManager);

            if (totalWages <= 0)
            {
                _dialogue.SendNPCMessage("All employees are already paid for today.");
                return;
            }

            _dialogue.SendNPCMessage($"Pay total wages for ${totalWages}?");
            _dialogue.ShowResponses(new DialogueResponse[]
            {
                new DialogueResponse("Yes", "Confirm", () =>
                {
                    if (WageUtils.TryPayAllUnpaidEmployees(employeeManager, out float cost, out string error))
                    {
                        _dialogue.SendPlayerMessage($"Paid ${cost} in wages.");
                        _dialogue.SendNPCMessage("All employees are now paid.");
                    }
                    else
                    {
                        _dialogue.SendNPCMessage($"Payment failed: {error}");
                    }
                    ShowPayOptions();
                }),
                new DialogueResponse("Back", "Back", ShowPayOptions)
            }, 0.3f);
        }),
        new DialogueResponse("Pay by property", "Per property", ShowPropertyPayList),
        new DialogueResponse("Back", "Back", Run)
    };

            _dialogue.ShowResponses(responses.ToArray(), 0.4f);
        }
        private void ShowPropertyPayList()
        {
            var responses = new List<DialogueResponse>();

            foreach (var prop in Property.OwnedProperties)
            {
                if (prop.PropertyName == null || prop.Employees == null || prop.Employees.Count == 0)
                    continue;

                float propWages = WageUtils.GetTotalUnpaidWagesForProperty(prop);
                string label = $"{prop.PropertyName} (${propWages})";

                responses.Add(new DialogueResponse(label, label, () =>
                {
                    if (propWages <= 0)
                    {
                        _dialogue.SendNPCMessage($"All employees at {prop.PropertyName} are paid.");
                        ShowPropertyPayList();
                        return;
                    }

                    _dialogue.SendNPCMessage($"Pay ${propWages} for {prop.PropertyName}?");
                    _dialogue.ShowResponses(new DialogueResponse[]
                    {

                new DialogueResponse("Yes", "Confirm", () =>
                {
                    if (WageUtils.TryPayUnpaidEmployeesForProperty(prop, out float cost, out string error))
                    {
                        _dialogue.SendPlayerMessage($"Paid ${cost} for {prop.PropertyName}.");
                        _dialogue.SendNPCMessage("Done.");
                    }
                    else
                    {
                        _dialogue.SendNPCMessage($"Failed: {error}");
                    }
                    ShowPropertyPayList();
                }),


                new DialogueResponse("Back", "Back", ShowPropertyPayList)
                    }, 0.3f);
                }));
            }

            responses.Add(new DialogueResponse("Back", "Back", ShowPayOptions));
            _dialogue.ShowResponses(responses.ToArray(), 0.4f);
        }

        private void ShowPropertyActions(Property prop)
        {
            var responses = new System.Collections.Generic.List<DialogueResponse>();

            // Base actions
            responses.Add(new DialogueResponse("Increase capacity", "Increase capacity", () =>
            {
                prop.EmployeeCapacity += 1;
                EmployeeConfigManager.SetCapacity(prop.PropertyName, prop.EmployeeCapacity);
                prop.EmployeeIdlePoints = GetIdleTransformsForProperty(prop.PropertyName, prop.EmployeeCapacity);

                _dialogue.SendPlayerMessage($"Increased capacity of {prop.PropertyName} to {prop.EmployeeCapacity}");
                _dialogue.SendNPCMessage($"Capacity is now {prop.EmployeeCapacity}");

                EmployeeConfigManager.SaveConfig();
                ShowPropertyActions(prop); // Refresh
            }));

            responses.Add(new DialogueResponse("List beds", "List beds", () =>
            {
                ShowPropertyEmployeesWithBeds(prop);
            }));

            // Hire employees
            AddHireOption("Hire Cleaner ($1,500)", 1500, "cleaner", prop, responses);
            AddHireOption("Hire Botanist ($1,500)", 1500,"botanist", prop, responses);
            AddHireOption("Hire Handler ($1,500)", 1500,"handler", prop, responses);
            AddHireOption("Hire Chemist ($2,000)", 2000,"chemist", prop, responses);

            //autotoggle payement
            string propKey = EmployeeConfigManager.Normalize(prop.PropertyName);
            bool current = EmployeeConfigManager.IsAutoPaymentEnabled(prop.PropertyName);
            string status = current ? "ON" : "OFF";

            responses.Add(new DialogueResponse(
              $"Toggle Auto-Payment (Currently {status})",
                $"Toggle Auto-Payment (Currently {status})",
                () =>
                {
                    float due = WageUtils.GetTotalUnpaidWagesForProperty(prop);
                    _dialogue.SendNPCMessage($"There are ${due} in unpaid wages.");

                    EmployeeConfigManager.ToggleAutoPayment(prop.PropertyName);

                    bool nowEnabled = EmployeeConfigManager.IsAutoPaymentEnabled(prop.PropertyName);
                    if (nowEnabled && due > 0f)
                    {
                        bool success = WageUtils.TryPayAllUnpaidEmployees(prop.Employees, out float paid, out string error);
                        if (success)
                        {
                            _dialogue.SendNPCMessage($"Paid ${paid} in wages for {prop.PropertyName}.");
                        }
                        else
                        {
                            _dialogue.SendNPCMessage($"Payment failed: {error}");
                        }
                    }

                    ShowPropertyActions(prop); // Refresh UI
                }
            ));

            // Back
            responses.Add(new DialogueResponse("Back", "Back", ShowOwnedPropertiesMenu, true));

            _dialogue.ShowResponses(responses.ToArray(), 0.5f);
        }
        private void AddHireOption(string label, int fee, string employeeType, Property prop, List<DialogueResponse> responses)
        {
            responses.Add(new DialogueResponse(label, label, () =>
            {
                _dialogue.SendNPCMessage($"Hiring a {employeeType} at {prop.PropertyName} will cost you ${fee}. Do you want to proceed?");
                _dialogue.ShowResponses(new DialogueResponse[]
                {
            new DialogueResponse("Yes, hire", "Confirm hire", () =>
            {
                var money = NetworkSingleton<MoneyManager>.Instance.cashBalance;
                if (money < fee)
                {
                    _dialogue.SendNPCMessage($"You don't have enough money. Required: ${fee}");
                    return;
                }

                // Deduct money and hire
                NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-fee, true, false);

                // Normalize property name by removing spaces
                string normalizedPropName = prop.PropertyName.Replace(" ", "").ToLowerInvariant();
                string command = $"addemployee {employeeType} {normalizedPropName}";
                Il2CppScheduleOne.Console.SubmitCommand(command);

                _dialogue.SendPlayerMessage($"Hired new {employeeType} at {prop.PropertyName} for ${fee}");
                _dialogue.SendNPCMessage("The employee is on their way.");

                ShowPropertyActions(prop); // Refresh
            }),
            new DialogueResponse("No", "Cancel", () =>
            {
                _dialogue.SendPlayerMessage("Cancelled hiring.");
                ShowPropertyActions(prop); // Go back to property menu
            })
                }, 0.3f);
            }));
        }

        public void Cleanup()
        {
            if (_npcObject != null)
                Object.Destroy(_npcObject);
            if (_npc != null && NPCManager.NPCRegistry.Contains(_npc))
                NPCManager.NPCRegistry.Remove(_npc);
        }
    }
}