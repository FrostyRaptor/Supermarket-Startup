using System;
using System.Collections.Generic;
using UnityEngine;
using Supermarket.Config;
using Supermarket.Core;
using Supermarket.Employees;
using Supermarket.Store;

namespace Supermarket.Products
{
    [Serializable]
    public class StockEntry
    {
        public string id;
        public int amount;

        public StockEntry() { }
        public StockEntry(string id, int amount) { this.id = id; this.amount = amount; }
    }

    [Serializable]
    public class Delivery
    {
        public string productId;
        public int quantity;
        public int arrivalDay;
        public int stall = -1;
    }

    /// <summary>
    /// All product stock: storage racks, general/alcohol shelves, and finished
    /// goods from the staffed sections. Trucks queue when there are no unloaders.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        public ProductCatalog catalog;
        public SectionManager sections;
        public EmployeeManager employees;
        public UpgradeSystem upgrades;
        public NotificationCenter notices;

        public List<StockEntry> storage = new List<StockEntry>();
        public List<StockEntry> shelf = new List<StockEntry>();
        public List<StockEntry> finished = new List<StockEntry>();
        public List<Delivery> deliveries = new List<Delivery>();

        readonly Dictionary<string, int> _storage = new Dictionary<string, int>();
        readonly Dictionary<string, int> _shelf = new Dictionary<string, int>();
        readonly Dictionary<string, int> _finished = new Dictionary<string, int>();

        public event Action Changed;

        const int StorageShelves = 3;

        void Awake()
        {
            RebuildCaches();
        }

        void RebuildCaches()
        {
            _storage.Clear();
            _shelf.Clear();
            _finished.Clear();
            CopyTo(storage, _storage);
            CopyTo(shelf, _shelf);
            CopyTo(finished, _finished);
        }

        static void CopyTo(List<StockEntry> source, Dictionary<string, int> target)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                StockEntry e = source[i];
                if (e == null || string.IsNullOrEmpty(e.id)) continue;
                target[e.id] = Mathf.Max(0, e.amount);
            }
        }

        public int StorageCount(string id) { return Get(_storage, id); }
        public int ShelfCount(string id) { return Get(_shelf, id); }
        public int FinishedCount(string id) { return Get(_finished, id); }

        static int Get(Dictionary<string, int> source, string id)
        {
            int value;
            return string.IsNullOrEmpty(id) ? 0 : (source.TryGetValue(id, out value) ? value : 0);
        }

        static void Set(Dictionary<string, int> source, string id, int value)
        {
            if (string.IsNullOrEmpty(id)) return;
            source[id] = Mathf.Max(0, value);
        }

        public void AddStorage(string id, int amount) { Set(_storage, id, StorageCount(id) + amount); Raise(); }
        public void AddShelf(string id, int amount) { Set(_shelf, id, ShelfCount(id) + amount); Raise(); }
        public void AddFinished(string id, int amount) { Set(_finished, id, FinishedCount(id) + amount); Raise(); }

        public int TakeFromStorage(string id, int amount)
        {
            int taken = Mathf.Min(Mathf.Max(0, amount), StorageCount(id));
            if (taken > 0) Set(_storage, id, StorageCount(id) - taken);
            return taken;
        }

        public int TakeFromShelf(string id, int amount)
        {
            int taken = Mathf.Min(Mathf.Max(0, amount), ShelfCount(id));
            if (taken > 0) Set(_shelf, id, ShelfCount(id) - taken);
            return taken;
        }

        public int TakeFromFinished(string id, int amount)
        {
            int taken = Mathf.Min(Mathf.Max(0, amount), FinishedCount(id));
            if (taken > 0) Set(_finished, id, FinishedCount(id) - taken);
            return taken;
        }

        public int TotalOnHand(string id)
        {
            return StorageCount(id) + ShelfCount(id) + FinishedCount(id) + IncomingQuantity(id);
        }

        public int IncomingQuantity(string id)
        {
            int total = 0;
            for (int i = 0; i < deliveries.Count; i++)
            {
                Delivery d = deliveries[i];
                if (d != null && d.productId == id && d.stall < 0) total += d.quantity;
            }
            return total;
        }

        public int StorageCapacity
        {
            get
            {
                if (upgrades == null || upgrades.StorageShelfProfile == null) return StorageShelves * 40;
                int total = 0;
                for (int i = 0; i < StorageShelves; i++)
                    total += upgrades.StorageShelfProfile.Capacity(upgrades.GetLevel(UpgradeSystem.StorageShelfKeyPrefix + i));
                return total;
            }
        }

        public int StorageUsed
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<string, int> pair in _storage) total += pair.Value;
                return total;
            }
        }

        public void ScheduleDelivery(string productId, int quantity, int arrivalDay)
        {
            if (string.IsNullOrEmpty(productId) || quantity <= 0) return;
            deliveries.Add(new Delivery { productId = productId, quantity = quantity, arrivalDay = arrivalDay, stall = -1 });
            Raise();
        }

        public int ActiveTrucks
        {
            get
            {
                int count = 0;
                for (int i = 0; i < deliveries.Count; i++)
                    if (deliveries[i] != null && deliveries[i].stall >= 0) count++;
                return count;
            }
        }

        /// <summary>Trucks take a stall when they arrive; extra deliveries wait outside.</summary>
        public void ProcessArrivals(int businessDay)
        {
            int stalls = sections == null ? 1 : sections.LoadingZones;
            for (int i = 0; i < deliveries.Count; i++)
            {
                Delivery d = deliveries[i];
                if (d == null || d.stall >= 0 || d.arrivalDay > businessDay) continue;
                if (ActiveTrucks >= stalls) break;
                d.stall = FreeStall(stalls);
                if (notices != null)
                    notices.Notify("A delivery of " + ProductName(d.productId) + " has arrived.", NoticeKind.Info);
            }
            Raise();
        }

        int FreeStall(int stalls)
        {
            bool[] used = new bool[stalls];
            for (int i = 0; i < deliveries.Count; i++)
            {
                Delivery d = deliveries[i];
                if (d != null && d.stall >= 0 && d.stall < stalls) used[d.stall] = true;
            }
            for (int i = 0; i < stalls; i++) if (!used[i]) return i;
            return 0;
        }

        /// <summary>Unloaders move goods from stalled trucks into storage racks.</summary>
        public void ProcessUnloading()
        {
            if (employees == null) return;
            int unloaders = employees.Count(EmployeeRole.Unloader);
            if (unloaders <= 0) return;

            float efficiency = 1f;
            if (upgrades != null && upgrades.LoadingZoneProfile != null)
            {
                int zoneLevel = upgrades.GetLevel(UpgradeSystem.LoadingZoneKeyPrefix + "0");
                efficiency = upgrades.LoadingZoneProfile.Efficiency(zoneLevel);
            }

            int budget = Mathf.RoundToInt(unloaders * 8f * efficiency);
            for (int i = 0; i < deliveries.Count && budget > 0; i++)
            {
                Delivery d = deliveries[i];
                if (d == null || d.stall < 0) continue;
                int room = Mathf.Max(0, StorageCapacity - StorageUsed);
                int moved = Mathf.Min(Mathf.Min(d.quantity, budget), room);
                if (moved < d.quantity && room <= 0)
                {
                    if (notices != null) notices.Notify("Storage racks are full.", NoticeKind.Warning);
                    break;
                }
                if (moved > 0)
                {
                    AddStorage(d.productId, moved);
                    d.quantity -= moved;
                    budget -= moved;
                }
            }

            for (int i = deliveries.Count - 1; i >= 0; i--)
                if (deliveries[i] != null && deliveries[i].stall >= 0 && deliveries[i].quantity <= 0) deliveries.RemoveAt(i);
            Raise();
        }

        /// <summary>Stockers move storage goods onto general/alcohol shelves.</summary>
        public void ProcessStocking()
        {
            if (employees == null || catalog == null) return;
            int stockers = employees.Count(EmployeeRole.Stocker);
            if (stockers <= 0) return;

            int budget = stockers * 12;
            for (int i = 0; i < catalog.products.Count && budget > 0; i++)
            {
                ProductInfo p = catalog.products[i];
                if (p == null || p.kind != ProductKind.Shelf) continue;
                if (sections != null && !sections.IsUnlocked(p.section)) continue;

                int cap = PerProductShelfCap(p.section);
                int need = Mathf.Max(0, cap - ShelfCount(p.id));
                if (need <= 0) continue;
                int moved = TakeFromStorage(p.id, Mathf.Min(need, budget));
                if (moved > 0)
                {
                    AddShelf(p.id, moved);
                    budget -= moved;
                }
            }
        }

        int PerProductShelfCap(StoreSectionType section)
        {
            int total = upgrades == null ? 20 : upgrades.TotalShelfCapacity(section);
            if (total <= 0) return 20;
            int products = 0;
            if (catalog != null)
            {
                for (int i = 0; i < catalog.products.Count; i++)
                {
                    ProductInfo p = catalog.products[i];
                    if (p != null && p.kind == ProductKind.Shelf && p.section == section) products++;
                }
            }
            return Mathf.Max(4, total / Mathf.Max(1, products));
        }

        /// <summary>Butcher/bakery/deli staff turn raw storage goods into finished stock.</summary>
        public void ProcessProduction()
        {
            if (employees == null || catalog == null) return;
            for (int i = 0; i < catalog.products.Count; i++)
            {
                ProductInfo p = catalog.products[i];
                if (p == null || p.kind != ProductKind.Finished) continue;
                if (sections != null && !sections.IsUnlocked(p.section)) continue;
                if (!employees.IsSectionStaffed(p.section)) continue;
                if (string.IsNullOrEmpty(p.inputProductId)) continue;

                EmployeeRole role = RoleFor(p.section);
                int staff = employees.Count(role);
                if (staff <= 0) continue;

                float efficiency = 1f;
                int yieldPer = 1;
                if (upgrades != null)
                {
                    UpgradeProfile profile = upgrades.ProfileFor(p.section);
                    if (profile != null)
                    {
                        efficiency = profile.Efficiency(upgrades.GetLevel(UpgradeSystem.EquipmentKey(p.section)));
                        yieldPer = profile.Yield(upgrades.GetLevel(UpgradeSystem.EquipmentKey(p.section)));
                    }
                }

                int rawAvailable = StorageCount(p.inputProductId) / Mathf.Max(1, p.inputUnits);
                int produce = Mathf.Min(rawAvailable, staff * yieldPer * Mathf.Max(1, Mathf.RoundToInt(efficiency)));
                if (produce <= 0) continue;
                TakeFromStorage(p.inputProductId, produce * Mathf.Max(1, p.inputUnits));
                AddFinished(p.id, produce);
            }
        }

        static EmployeeRole RoleFor(StoreSectionType section)
        {
            switch (section)
            {
                case StoreSectionType.Butcher: return EmployeeRole.Butcher;
                case StoreSectionType.Bakery: return EmployeeRole.Bakery;
                case StoreSectionType.Deli: return EmployeeRole.Deli;
                default: return EmployeeRole.Stocker;
            }
        }

        string ProductName(string id)
        {
            ProductInfo p = catalog == null ? null : catalog.Get(id);
            return p == null ? id : p.displayName;
        }

        public void SyncLists()
        {
            WriteList(storage, _storage);
            WriteList(shelf, _shelf);
            WriteList(finished, _finished);
        }

        static void WriteList(List<StockEntry> list, Dictionary<string, int> source)
        {
            list.Clear();
            foreach (KeyValuePair<string, int> pair in source)
                if (pair.Value > 0) list.Add(new StockEntry(pair.Key, pair.Value));
        }

        public void Restore(List<StockEntry> stored, List<StockEntry> shelved, List<StockEntry> made, List<Delivery> pending)
        {
            storage = stored ?? new List<StockEntry>();
            shelf = shelved ?? new List<StockEntry>();
            finished = made ?? new List<StockEntry>();
            deliveries = pending ?? new List<Delivery>();
            RebuildCaches();
            Raise();
        }

        void Raise()
        {
            if (Changed != null) Changed();
        }
    }
}
