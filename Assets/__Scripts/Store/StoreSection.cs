using UnityEngine;
using Supermarket.Config;

namespace Supermarket.Store
{
    /// <summary>
    /// Marker/data component placed on each preset section's wall group in the scene.
    /// The general section is the buildable grid; all others have fixed slots.
    /// </summary>
    public class StoreSection : MonoBehaviour
    {
        public StoreSectionType type;
        public string displayName;
        public bool startsUnlocked;
        public Transform[] slots = new Transform[0];

        public int SlotCount { get { return slots == null ? 0 : slots.Length; } }

        public Vector3 SlotPosition(int index)
        {
            if (slots == null || slots.Length == 0) return transform.position;
            int i = Mathf.Clamp(index, 0, slots.Length - 1);
            return slots[i] != null ? slots[i].position : transform.position;
        }
    }
}
