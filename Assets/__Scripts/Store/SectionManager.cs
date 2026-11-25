using System;
using System.Collections.Generic;
using UnityEngine;
using Supermarket.Building;
using Supermarket.Config;
using Supermarket.Core;
using GameWallet = Supermarket.Economy.Economy;

namespace Supermarket.Store
{
    /// <summary>
    /// Owns which sections are unlocked and how many preset slots the player has
    /// purchased (checkout lanes and loading zones).
    /// </summary>
    public class SectionManager : MonoBehaviour
    {
        public SectionConfig config;
        public GameWallet economy;
        public NotificationCenter notices;

        [Header("Build System")]
        [SerializeField] BuildGrid _buildGrid;

        readonly Dictionary<StoreSectionType, bool> _unlocked = new Dictionary<StoreSectionType, bool>();
        readonly List<StoreSection> _sections = new List<StoreSection>();
        readonly List<PlacedObject> _placedScratch = new List<PlacedObject>();

        int _checkoutLanes = 1;
        int _loadingZones = 1;
        bool _gridHooked;

        public event Action Changed;

        /// <summary>Grid that owns the placed checkout lanes. Legacy count is used when absent.</summary>
        public BuildGrid buildGrid
        {
            get { return _buildGrid; }
            set
            {
                if (_buildGrid == value) return;
                UnhookGrid();
                _buildGrid = value;
                HookGrid();
                RaiseChanged();
            }
        }

        /// <summary>Count of checkout lanes actually placed in the checkout section.</summary>
        public int CheckoutLanes
        {
            get
            {
                if (_buildGrid == null) return _checkoutLanes;

                _buildGrid.GetAllPlacedObjects(_placedScratch);
                int lanes = 0;
                for (int i = 0; i < _placedScratch.Count; i++)
                {
                    if (IsCheckoutLane(_placedScratch[i])) lanes++;
                }
                return lanes;
            }
        }

        public int LoadingZones { get { return _loadingZones; } }
        public int MaxCheckoutLanes { get { return config == null ? 4 : Mathf.Max(1, config.maxCheckoutLanes); } }
        public int MaxLoadingZones { get { return config == null ? 3 : Mathf.Max(1, config.maxLoadingZones); } }
        public IReadOnlyList<StoreSection> Sections { get { return _sections; } }

        void Awake()
        {
            ResetToDefaults();
        }

        void OnEnable()
        {
            HookGrid();
        }

        void OnDisable()
        {
            UnhookGrid();
        }

        void HookGrid()
        {
            if (_gridHooked || _buildGrid == null) return;
            _buildGrid.ObjectPlaced += OnBuildObjectChanged;
            _buildGrid.ObjectRemoved += OnBuildObjectChanged;
            _gridHooked = true;
        }

        void UnhookGrid()
        {
            if (!_gridHooked || _buildGrid == null) return;
            _buildGrid.ObjectPlaced -= OnBuildObjectChanged;
            _buildGrid.ObjectRemoved -= OnBuildObjectChanged;
            _gridHooked = false;
        }

        void OnBuildObjectChanged(PlacedObject placed)
        {
            if (IsCheckoutLane(placed)) RaiseChanged();
        }

        static bool IsCheckoutLane(PlacedObject placed)
        {
            return placed != null && placed.Definition != null &&
                   placed.Definition.allowedArea == PlaceableArea.Checkout;
        }

        /// <summary>Notifies listeners that the placed lane count changed (e.g. after a layout load).</summary>
        public void NotifyLaneCountChanged()
        {
            RaiseChanged();
        }

        void SetDefaultUnlocks()
        {
            _unlocked.Clear();
            _unlocked[StoreSectionType.General] = true;
            _unlocked[StoreSectionType.Storage] = true;
            _unlocked[StoreSectionType.Checkout] = true;
            _unlocked[StoreSectionType.Butcher] = false;
            _unlocked[StoreSectionType.Bakery] = false;
            _unlocked[StoreSectionType.Deli] = false;
            _unlocked[StoreSectionType.Alcohol] = false;
        }

        /// <summary>
        /// EditMode tests and editor tooling can call methods without Awake having
        /// run. Keep the baseline unlocks available in that case too.
        /// </summary>
        void EnsureDefaults()
        {
            if (_unlocked.Count == 0) SetDefaultUnlocks();
        }

        public void ResetToDefaults()
        {
            SetDefaultUnlocks();

            _sections.Clear();
            StoreSection[] found = FindObjectsByType<StoreSection>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                StoreSection s = found[i];
                _sections.Add(s);
                if (s.startsUnlocked) _unlocked[s.type] = true;
            }

            _checkoutLanes = 1;
            _loadingZones = 1;
            RaiseChanged();
        }

        public bool IsUnlocked(StoreSectionType type)
        {
            EnsureDefaults();
            bool value;
            return _unlocked.TryGetValue(type, out value) && value;
        }

        public int UnlockCost(StoreSectionType type)
        {
            if (config == null) return 0;
            SectionLayoutInfo layout = config.Get(type);
            return layout == null ? 0 : Mathf.Max(0, layout.unlockCost);
        }

        public bool TryUnlock(StoreSectionType type)
        {
            if (IsUnlocked(type)) return false;
            int cost = UnlockCost(type);
            if (economy != null && !economy.CanAfford(cost))
            {
                Notify("Not enough money to unlock " + type + ".", NoticeKind.Warning);
                return false;
            }
            if (economy != null) economy.TrySpend(cost);
            _unlocked[type] = true;
            Notify(type + " section unlocked!", NoticeKind.Success);
            RaiseChanged();
            return true;
        }

        public bool TryExpandCheckout()
        {
            // With the build grid wired, lanes are placed in build mode instead of
            // being purchased as an abstract counter.
            if (_buildGrid != null) return false;
            if (_checkoutLanes >= MaxCheckoutLanes) return false;
            int cost = config == null ? 0 : config.checkoutLaneCost;
            if (economy != null && !economy.CanAfford(cost))
            {
                Notify("Not enough money for another checkout lane.", NoticeKind.Warning);
                return false;
            }
            if (economy != null) economy.TrySpend(cost);
            _checkoutLanes++;
            Notify("Checkout lane " + _checkoutLanes + " is open.", NoticeKind.Success);
            RaiseChanged();
            return true;
        }

        public bool TryExpandLoadingZone()
        {
            if (_loadingZones >= MaxLoadingZones) return false;
            int cost = config == null ? 0 : config.loadingZoneCost;
            if (economy != null && !economy.CanAfford(cost))
            {
                Notify("Not enough money for another loading zone.", NoticeKind.Warning);
                return false;
            }
            if (economy != null) economy.TrySpend(cost);
            _loadingZones++;
            Notify("Loading zone " + _loadingZones + " is open.", NoticeKind.Success);
            RaiseChanged();
            return true;
        }

        public Vector3 SlotPosition(StoreSectionType type, int index)
        {
            StoreSection section = FindSection(type);
            if (section != null && section.SlotCount > 0) return section.SlotPosition(index);
            return config == null ? Vector3.zero : config.SlotPosition(type, index);
        }

        public StoreSection FindSection(StoreSectionType type)
        {
            for (int i = 0; i < _sections.Count; i++)
                if (_sections[i] != null && _sections[i].type == type) return _sections[i];
            return null;
        }

        public void Restore(Dictionary<StoreSectionType, bool> unlocked, int lanes, int zones)
        {
            EnsureDefaults();
            if (unlocked != null)
            {
                foreach (KeyValuePair<StoreSectionType, bool> pair in unlocked) _unlocked[pair.Key] = pair.Value;
            }
            _checkoutLanes = Mathf.Clamp(lanes, 0, MaxCheckoutLanes);
            _loadingZones = Mathf.Clamp(zones, 1, MaxLoadingZones);
            RaiseChanged();
        }

        public Dictionary<StoreSectionType, bool> Snapshot()
        {
            EnsureDefaults();
            return new Dictionary<StoreSectionType, bool>(_unlocked);
        }

        void Notify(string message, NoticeKind kind)
        {
            if (notices != null) notices.Notify(message, kind);
        }

        void RaiseChanged()
        {
            if (Changed != null) Changed();
        }
    }
}
