using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SharedModUtils
{
    public static class WageUtils
    {
        public static bool TryPayAllUnpaidEmployees(EmployeeManager employeeManager, out float totalCost, out string error)
        {
            totalCost = 0f;
            error = null;

            if (employeeManager == null)
            {
                error = "Employee manager is not available.";
                return false;
            }

            for (int i = 0; i < employeeManager.AllEmployees._size; i++)
            {
                var emp = employeeManager.AllEmployees[i];
                if (!emp.PaidForToday)
                {
                    totalCost += emp.DailyWage;
                }
            }

            var moneyManager = Il2CppScheduleOne.Money.MoneyManager.instance;
            float currentBalance = moneyManager.cashBalance;

            if (totalCost > currentBalance)
            {
                error = $"Wages (${totalCost}) exceed bank balance (${currentBalance}).";
                return false;
            }

            for (int i = 0; i < employeeManager.AllEmployees._size; i++)
            {
                employeeManager.AllEmployees[i].PaidForToday = true;
            }

            NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-totalCost, true, false);

            return true;
        }
        public static float GetTotalUnpaidWages(EmployeeManager employeeMgr)
        {
            if (employeeMgr == null || employeeMgr.AllEmployees == null)
                return 0f;

            float total = 0f;
            for (int i = 0; i < employeeMgr.AllEmployees._size; i++)
            {
                var employee = employeeMgr.AllEmployees[i];
                if (employee != null && !employee.PaidForToday)
                {
                    total += employee.DailyWage;
                }
            }

            return total;
        }
        public static float GetTotalUnpaidWagesForProperty(Property prop)
        {
            float total = 0f;
            foreach (var emp in prop.Employees)
            {
                if (!emp.PaidForToday)
                    total += emp.DailyWage;
            }
            return total;
        }

        public static bool TryPayUnpaidEmployeesForProperty(Property prop, out float cost, out string error)
        {
            cost = GetTotalUnpaidWagesForProperty(prop);
            error = "";

            if (cost <= 0) return true;

            var cash = NetworkSingleton<MoneyManager>.Instance.cashBalance;
            if (cash < cost)
            {
                error = "Not enough cash.";
                return false;
            }

            NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-cost, true, false);
            foreach (var emp in prop.Employees)
            {
                if (!emp.PaidForToday)
                    emp.SetIsPaid();
            }

            return true;
        }

        public static bool TryPayAllUnpaidEmployees(Il2CppSystem.Collections.Generic.List<Employee> employees, out float totalCost, out string error)
        {
            totalCost = 0f;
            error = null;

            foreach (var emp in employees)
            {
                if (!emp.PaidForToday)
                    totalCost += emp.DailyWage;
            }

            float balance = MoneyManager.instance.cashBalance;
            if (totalCost > balance)
            {
                error = $"Insufficient funds: need ${totalCost}, have ${(int) balance}";
                return false;
            }

            foreach (var emp in employees)
                emp.PaidForToday = true;

            NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-totalCost, true, false);
            return true;
        }


    }

}