using UnityEngine;

namespace Supermarket.Config
{
    /// <summary>Daily wages and section head-count limits, editable in the Inspector.</summary>
    [CreateAssetMenu(menuName = "Supermarket/Employee Config", fileName = "EmployeeConfig")]
    public class EmployeeConfig : ScriptableObject
    {
        [Header("Daily wages")]
        public int checkoutWage = 90;
        public int butcherWage = 110;
        public int bakeryWage = 110;
        public int deliWage = 110;
        public int unloaderWage = 85;
        public int stockerWage = 80;

        [Header("Maximum head counts (checkout is dynamic: one per lane)")]
        public int butcherMax = 5;
        public int bakeryMax = 5;
        public int deliMax = 5;
        public int unloaderMax = 6;
        public int stockerMax = 8;

        public int WageFor(Employees.EmployeeRole role)
        {
            switch (role)
            {
                case Employees.EmployeeRole.Checkout: return checkoutWage;
                case Employees.EmployeeRole.Butcher: return butcherWage;
                case Employees.EmployeeRole.Bakery: return bakeryWage;
                case Employees.EmployeeRole.Deli: return deliWage;
                case Employees.EmployeeRole.Unloader: return unloaderWage;
                case Employees.EmployeeRole.Stocker: return stockerWage;
                default: return 0;
            }
        }

        public int MaxFor(Employees.EmployeeRole role)
        {
            switch (role)
            {
                case Employees.EmployeeRole.Butcher: return butcherMax;
                case Employees.EmployeeRole.Bakery: return bakeryMax;
                case Employees.EmployeeRole.Deli: return deliMax;
                case Employees.EmployeeRole.Unloader: return unloaderMax;
                case Employees.EmployeeRole.Stocker: return stockerMax;
                default: return 0;
            }
        }
    }
}
