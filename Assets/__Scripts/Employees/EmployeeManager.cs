using System;
using System.Collections.Generic;
using UnityEngine;
using Supermarket.Config;
using Supermarket.Core;
using Supermarket.Store;

namespace Supermarket.Employees
{
    /// <summary>
    /// Hire/fire state, dynamic caps, daily wages and the zero-staffing rules
    /// that close sections or stop deliveries/restocking.
    /// </summary>
    public class EmployeeManager : MonoBehaviour
    {
        public EmployeeConfig config;
        public SectionManager sections;
        public NotificationCenter notices;

        readonly Dictionary<EmployeeRole, int> _counts = new Dictionary<EmployeeRole, int>();
        readonly Dictionary<string, bool> _warned = new Dictionary<string, bool>();

        public event Action CountsChanged;

        static readonly EmployeeRole[] Roles =
        {
            EmployeeRole.Checkout, EmployeeRole.Butcher, EmployeeRole.Bakery,
            EmployeeRole.Deli, EmployeeRole.Unloader, EmployeeRole.Stocker
        };

        void Awake()
        {
            for (int i = 0; i < Roles.Length; i++) _counts[Roles[i]] = 0;
        }

        public int Count(EmployeeRole role)
        {
            int value;
            return _counts.TryGetValue(role, out value) ? value : 0;
        }

        public int Max(EmployeeRole role)
        {
            if (role == EmployeeRole.Checkout)
                return sections == null ? 1 : sections.CheckoutLanes;
            return config == null ? 0 : config.MaxFor(role);
        }

        public bool TryHire(EmployeeRole role)
        {
            if (Count(role) >= Max(role)) return false;
            _counts[role] = Count(role) + 1;
            EvaluateStaffing(false);
            RaiseChanged();
            return true;
        }

        public bool TryFire(EmployeeRole role)
        {
            if (Count(role) <= 0) return false;
            _counts[role] = Count(role) - 1;
            EvaluateStaffing(true);
            RaiseChanged();
            return true;
        }

        public int DailyWages
        {
            get
            {
                int total = 0;
                if (config == null) return 0;
                for (int i = 0; i < Roles.Length; i++)
                    total += Count(Roles[i]) * config.WageFor(Roles[i]);
                return total;
            }
        }

        /// <summary>Checkout must have both a lane and an employee.</summary>
        public bool CheckoutOpen
        {
            get
            {
                return (sections == null || sections.CheckoutLanes > 0) && Count(EmployeeRole.Checkout) > 0;
            }
        }

        public bool IsSectionStaffed(StoreSectionType type)
        {
            switch (type)
            {
                case StoreSectionType.General:
                case StoreSectionType.Alcohol:
                    return Count(EmployeeRole.Stocker) > 0;
                case StoreSectionType.Butcher: return Count(EmployeeRole.Butcher) > 0;
                case StoreSectionType.Bakery: return Count(EmployeeRole.Bakery) > 0;
                case StoreSectionType.Deli: return Count(EmployeeRole.Deli) > 0;
                case StoreSectionType.Checkout: return CheckoutOpen;
                default: return true;
            }
        }

        public bool CanSell(StoreSectionType type)
        {
            if (!CheckoutOpen) return false;
            if (sections != null && !sections.IsUnlocked(type)) return false;
            if (type == StoreSectionType.General || type == StoreSectionType.Alcohol)
                return Count(EmployeeRole.Stocker) > 0;
            return IsSectionStaffed(type);
        }

        /// <summary>Raises each warning only on the transition into the bad state.</summary>
        public void EvaluateStaffing(bool allowRepeat)
        {
            CheckWarn("checkout", !CheckoutOpen,
                "Checkout is closed: you need at least one checkout lane and one checkout employee.", allowRepeat);

            CheckSectionWarning(StoreSectionType.Butcher, EmployeeRole.Butcher,
                "The butcher is closed due to low staffing.", allowRepeat);
            CheckSectionWarning(StoreSectionType.Bakery, EmployeeRole.Bakery,
                "The bakery is closed due to low staffing.", allowRepeat);
            CheckSectionWarning(StoreSectionType.Deli, EmployeeRole.Deli,
                "The deli is closed due to low staffing.", allowRepeat);

            bool storageOpen = sections == null || sections.IsUnlocked(StoreSectionType.Storage);
            CheckWarn("unloader", storageOpen && Count(EmployeeRole.Unloader) == 0,
                "Trucks aren't getting unloaded because you have no unloaders.", allowRepeat);
            CheckWarn("stocker", storageOpen && Count(EmployeeRole.Stocker) == 0,
                "Shelves will not be stocked because you have no stockers.", allowRepeat);
        }

        void CheckSectionWarning(StoreSectionType type, EmployeeRole role, string message, bool allowRepeat)
        {
            bool unlocked = sections == null || sections.IsUnlocked(type);
            CheckWarn(type.ToString(), unlocked && Count(role) == 0, message, allowRepeat);
        }

        void CheckWarn(string key, bool bad, string message, bool allowRepeat)
        {
            bool wasBad;
            bool known = _warned.TryGetValue(key, out wasBad);
            _warned[key] = bad;
            if (bad && (!known || !wasBad || allowRepeat)) Notify(message, NoticeKind.Warning);
        }

        public void Restore(Dictionary<EmployeeRole, int> counts)
        {
            if (counts == null) return;
            foreach (KeyValuePair<EmployeeRole, int> pair in counts) _counts[pair.Key] = Mathf.Max(0, pair.Value);
            _warned.Clear();
            RaiseChanged();
        }

        public Dictionary<EmployeeRole, int> Snapshot()
        {
            return new Dictionary<EmployeeRole, int>(_counts);
        }

        void Notify(string message, NoticeKind kind)
        {
            if (notices != null) notices.Notify(message, kind);
        }

        void RaiseChanged()
        {
            if (CountsChanged != null) CountsChanged();
        }
    }
}
