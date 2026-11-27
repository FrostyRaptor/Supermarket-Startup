using UnityEngine;

namespace Supermarket.Store
{
    /// <summary>
    /// Keeps a loading-bay door texture in sync with the number of loading zones
    /// the player has purchased. The light in the texture is only painted art;
    /// this component never creates a runtime light.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoadingDockDoorStatus : MonoBehaviour
    {
        [Header("State")]
        public SectionManager sections;
        [Range(1, 3)] public int dockNumber = 1;

        [Header("Visuals")]
        public Material purchasedMaterial;
        public Material unpurchasedMaterial;

        MeshRenderer _renderer;
        bool _subscribed;

        void OnEnable()
        {
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            if (sections == null) sections = FindFirstObjectByType<SectionManager>();

            if (sections != null && !_subscribed)
            {
                sections.Changed += OnSectionsChanged;
                _subscribed = true;
            }

            Refresh();
        }

        void OnDisable()
        {
            if (!_subscribed || sections == null) return;
            sections.Changed -= OnSectionsChanged;
            _subscribed = false;
        }

        void OnValidate()
        {
            dockNumber = Mathf.Clamp(dockNumber, 1, 3);
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            Refresh();
        }

        void OnSectionsChanged()
        {
            Refresh();
        }

        /// <summary>Applies the purchased or unpurchased texture immediately.</summary>
        public void Refresh()
        {
            if (_renderer == null || sections == null) return;

            bool purchased = dockNumber <= Mathf.Max(1, sections.LoadingZones);
            Material target = purchased ? purchasedMaterial : unpurchasedMaterial;
            if (target != null) _renderer.sharedMaterial = target;
        }
    }
}
