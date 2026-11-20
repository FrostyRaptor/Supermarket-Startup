using System;
using System.Collections.Generic;
using UnityEngine;
using Supermarket.Config;
using Supermarket.Core;
using GameWallet = Supermarket.Economy.Economy;
using Supermarket.Building;

namespace Supermarket.Store
{
    /// <summary>
    /// Tracks upgrade levels for fixed equipment (checkout lanes, loading zones,
    /// storage shelves and the butcher/bakery/deli equipment) and for placed
    /// furniture fixtures.
    /// </summary>
    public class UpgradeSystem : MonoBehaviour
    {
        public GameWallet economy;
        public NotificationCenter notices;
        public BuildGrid grid;

        readonly Dictionary<string, int> _fixedLevels = new Dictionary<string, int>();
        readonly Dictionary<string, UpgradeableFixture> _fixtures = new Dictionary<string, UpgradeableFixture>();
        readonly Dictionary<string, FixtureSave> _pendingFixtures = new Dictionary<string, FixtureSave>();

        public event Action Changed;

        public const string CheckoutKeyPrefix = "checkout_lane_";
        public const string LoadingZoneKeyPrefix = "loading_zone_";
        public const string StorageShelfKeyPrefix = "storage_shelf_";
        public const string ButcherKey = "butcher_equipment";
        public const string BakeryKey = "bakery_equipment";
        public const string DeliKey = "deli_equipment";

        public static string EquipmentKey(StoreSectionType type)
        {
            switch (type)
            {
                case StoreSectionType.Butcher: return ButcherKey;
                case StoreSectionType.Bakery: return BakeryKey;
                case StoreSectionType.Deli: return DeliKey;
                default: return type.ToString().ToLowerInvariant();
            }
        }

        void Awake()
        {
            if (grid == null) grid = FindFirstObjectByType<BuildGrid>();
        }

        public int GetLevel(string key)
        {
            int value;
            return _fixedLevels.TryGetValue(key, out value) ? UpgradeProfile.ClampLevel(value) : 1;
        }

        public float CheckoutEfficiency(int laneIndex)
        {
            string key = CheckoutKeyPrefix + laneIndex;
            return LaneProfile != null ? LaneProfile.Efficiency(GetLevel(key)) : 1f;
        }

        public UpgradeProfile LaneProfile;
        public UpgradeProfile LoadingZoneProfile;
        public UpgradeProfile StorageShelfProfile;
        public UpgradeProfile ButcherProfile;
        public UpgradeProfile BakeryProfile;
        public UpgradeProfile DeliProfile;

        public UpgradeProfile ProfileFor(StoreSectionType type)
        {
            switch (type)
            {
                case StoreSectionType.Butcher: return ButcherProfile;
                case StoreSectionType.Bakery: return BakeryProfile;
                case StoreSectionType.Deli: return DeliProfile;
                case StoreSectionType.Checkout: return LaneProfile;
                case StoreSectionType.Storage: return StorageShelfProfile;
                default: return null;
            }
        }

        public bool TryUpgradeFixed(string key, UpgradeProfile profile, string label)
        {
            if (profile == null) return false;
            int level = GetLevel(key);
            if (level >= UpgradeProfile.MaxLevel) return false;
            int cost = profile.CostForNextLevel(level);
            if (economy != null && !economy.CanAfford(cost))
            {
                if (notices != null) notices.Notify("Not enough money to upgrade " + label + ".", NoticeKind.Warning);
                return false;
            }
            if (economy != null) economy.TrySpend(cost);
            _fixedLevels[key] = level + 1;
            if (notices != null) notices.Notify(label + " upgraded to level " + (level + 1) + ".", NoticeKind.Success);
            RaiseChanged();
            return true;
        }

        /// <summary>Called by GameManager whenever the build grid spawns an object.</summary>
        public void RegisterPlaced(PlacedObject placed)
        {
            if (placed == null || placed.Definition == null || placed.Definition.upgradeProfile == null) return;
            UpgradeableFixture fixture = placed.GetComponent<UpgradeableFixture>();
            if (fixture == null) fixture = placed.gameObject.AddComponent<UpgradeableFixture>();
            fixture.Initialize(placed, placed.Definition.upgradeProfile);
            _fixtures[fixture.key] = fixture;

            FixtureSave saved;
            if (_pendingFixtures.TryGetValue(fixture.key, out saved))
            {
                fixture.Restore(saved.key, saved.level, saved.stock);
                _pendingFixtures.Remove(fixture.key);
            }
            RaiseChanged();
        }

        public void ForgetPlaced(PlacedObject placed)
        {
            if (placed == null) return;
            string key = UpgradeableFixture.MakeKey(placed);
            _fixtures.Remove(key);
            RaiseChanged();
        }

        public IReadOnlyDictionary<string, UpgradeableFixture> Fixtures { get { return _fixtures; } }

        public int TotalShelfCapacity(StoreSectionType section)
        {
            int total = 0;
            foreach (KeyValuePair<string, UpgradeableFixture> pair in _fixtures)
            {
                UpgradeableFixture f = pair.Value;
                if (f == null || f.Placed == null || f.Placed.Definition == null) continue;
                if (f.Placed.Definition.category == PlaceableCategory.Decor) continue;
                if (section == StoreSectionType.General && !f.key.StartsWith("alcohol", StringComparison.Ordinal)) total += f.Capacity;
                else if (section == StoreSectionType.Alcohol && f.key.StartsWith("alcohol", StringComparison.Ordinal)) total += f.Capacity;
            }
            return total;
        }

        public void Restore(Dictionary<string, int> fixedLevels, List<FixtureSave> fixtures)
        {
            _fixedLevels.Clear();
            if (fixedLevels != null)
                foreach (KeyValuePair<string, int> pair in fixedLevels) _fixedLevels[pair.Key] = pair.Value;

            _pendingFixtures.Clear();
            if (fixtures != null)
            {
                for (int i = 0; i < fixtures.Count; i++)
                {
                    FixtureSave saved = fixtures[i];
                    if (saved == null || string.IsNullOrEmpty(saved.key)) continue;
                    UpgradeableFixture fixture;
                    if (_fixtures.TryGetValue(saved.key, out fixture)) fixture.Restore(saved.key, saved.level, saved.stock);
                    else _pendingFixtures[saved.key] = saved;
                }
            }
            RaiseChanged();
        }

        public Dictionary<string, int> SnapshotFixed()
        {
            return new Dictionary<string, int>(_fixedLevels);
        }

        void RaiseChanged()
        {
            if (Changed != null) Changed();
        }
    }

    [Serializable]
    public class FixtureSave
    {
        public string key;
        public int level;
        public int stock;

        public FixtureSave() { }

        public FixtureSave(string key, int level, int stock)
        {
            this.key = key;
            this.level = level;
            this.stock = stock;
        }
    }
}
