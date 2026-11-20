using UnityEngine;

namespace Supermarket.Config
{
    /// <summary>
    /// Five-level upgrade curve shared by furniture and employee-section equipment.
    /// Capacity, efficiency, yield, costs and level materials are all Inspector values.
    /// </summary>
    [CreateAssetMenu(menuName = "Supermarket/Upgrade Profile", fileName = "UpgradeProfile")]
    public class UpgradeProfile : ScriptableObject
    {
        public string id;
        public string displayName = "Equipment";

        [Tooltip("Index 0 = level 1 ... index 4 = level 5.")]
        public int[] capacityByLevel = { 8, 12, 18, 26, 36 };

        [Tooltip("Cost to move TO that level; index 0 is unused.")]
        public int[] upgradeCostByLevel = { 0, 150, 300, 600, 1000 };

        [Tooltip("Speed multiplier for employee equipment.")]
        public float[] efficiencyByLevel = { 1f, 1.3f, 1.7f, 2.2f, 3f };

        [Tooltip("Finished units produced per production action.")]
        public int[] yieldByLevel = { 1, 2, 3, 4, 5 };

        [Tooltip("Renderer material swapped in to show the current level.")]
        public Material[] levelMaterials;

        public const int MaxLevel = 5;

        public int Capacity(int level) { return Value(capacityByLevel, level, 8); }
        public int CostForNextLevel(int currentLevel)
        {
            int target = ClampLevel(currentLevel + 1);
            return Value(upgradeCostByLevel, target, 0);
        }
        public float Efficiency(int level) { return ValueFloat(efficiencyByLevel, level, 1f); }
        public int Yield(int level) { return Value(yieldByLevel, level, 1); }

        public Material MaterialFor(int level)
        {
            if (levelMaterials == null || levelMaterials.Length == 0) return null;
            int i = Mathf.Clamp(level - 1, 0, levelMaterials.Length - 1);
            return levelMaterials[i];
        }

        public static int ClampLevel(int level) { return Mathf.Clamp(level, 1, MaxLevel); }

        static int Value(int[] values, int level, int fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            int i = Mathf.Clamp(level - 1, 0, values.Length - 1);
            return Mathf.Max(0, values[i]);
        }

        static float ValueFloat(float[] values, int level, float fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            int i = Mathf.Clamp(level - 1, 0, values.Length - 1);
            return Mathf.Max(0f, values[i]);
        }
    }
}
