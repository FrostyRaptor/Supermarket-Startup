using UnityEngine;
using Supermarket.Building;
using Supermarket.Config;
using GameWallet = Supermarket.Economy.Economy;

namespace Supermarket.Store
{
    /// <summary>
    /// Added to a placed shelf/cooler/freezer (or alcohol shelf) so it can be
    /// upgraded 1-5. Capacity and visuals come from its UpgradeProfile.
    /// </summary>
    public class UpgradeableFixture : MonoBehaviour
    {
        public string key;
        public UpgradeProfile profile;
        public int level = 1;
        public int stock;

        PlacedObject _placed;

        public int Capacity { get { return profile == null ? 0 : profile.Capacity(level); } }
        public bool IsFull { get { return stock >= Capacity; } }
        public bool CanUpgrade { get { return level < UpgradeProfile.MaxLevel; } }

        public PlacedObject Placed
        {
            get
            {
                if (_placed == null) _placed = GetComponent<PlacedObject>();
                return _placed;
            }
        }

        public static string MakeKey(PlacedObject placed)
        {
            if (placed == null || placed.Definition == null) return "fixture";
            string id = placed.Definition.id;
            int x = placed.Origin.X;
            int z = placed.Origin.Z;
            int turns = placed.QuarterTurns;
            return string.Concat(id, "_", x, "_", z, "_", turns);
        }

        public void Initialize(PlacedObject placed, UpgradeProfile upgradeProfile)
        {
            _placed = placed;
            profile = upgradeProfile;
            if (string.IsNullOrEmpty(key)) key = MakeKey(placed);
            stock = 0;
            level = 1;
            ApplyMaterial();
        }

        public bool TryUpgrade(GameWallet economy)
        {
            if (profile == null || !CanUpgrade) return false;
            int cost = profile.CostForNextLevel(level);
            if (economy != null && !economy.CanAfford(cost)) return false;
            if (economy != null) economy.TrySpend(cost);
            level++;
            ApplyMaterial();
            return true;
        }

        public void ApplyMaterial()
        {
            if (profile == null) return;
            Material material = profile.MaterialFor(level);
            if (material == null) return;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].sharedMaterial = material;
        }

        public void Restore(string savedKey, int savedLevel, int savedStock)
        {
            if (!string.IsNullOrEmpty(savedKey)) key = savedKey;
            level = UpgradeProfile.ClampLevel(savedLevel);
            stock = Mathf.Max(0, savedStock);
            ApplyMaterial();
        }
    }
}
