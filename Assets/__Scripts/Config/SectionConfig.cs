using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Config
{
    [Serializable]
    public class SectionLayoutInfo
    {
        public StoreSectionType type;
        public string displayName;
        public int unlockCost;
        public Vector3 center;
        public Vector3[] slots = new Vector3[0];
    }

    /// <summary>Unlock costs and preset slot positions for the walled sections.</summary>
    [CreateAssetMenu(menuName = "Supermarket/Section Config", fileName = "SectionConfig")]
    public class SectionConfig : ScriptableObject
    {
        public List<SectionLayoutInfo> sections = new List<SectionLayoutInfo>();

        public int checkoutLaneCost = 250;
        public int loadingZoneCost = 300;
        public int maxCheckoutLanes = 4;
        public int maxLoadingZones = 3;

        public SectionLayoutInfo Get(StoreSectionType type)
        {
            for (int i = 0; i < sections.Count; i++)
            {
                SectionLayoutInfo s = sections[i];
                if (s != null && s.type == type) return s;
            }
            return null;
        }

        public Vector3 SlotPosition(StoreSectionType type, int slotIndex)
        {
            SectionLayoutInfo layout = Get(type);
            if (layout == null || layout.slots == null || layout.slots.Length == 0) return Vector3.zero;
            int i = Mathf.Clamp(slotIndex, 0, layout.slots.Length - 1);
            return layout.slots[i] + layout.center;
        }
    }
}
