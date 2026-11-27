using UnityEngine;
using UnityEngine.UI;

namespace Supermarket.UI
{
    /// <summary>
    /// Static contents of the authored Upgrades tab. The five section title rows
    /// are Storage, Butcher, Bakery, Deli, and Alcohol, in that order. Every
    /// upgrade row below a title belongs to that section.
    /// </summary>
    public class HudUpgradesView : MonoBehaviour
    {
        [Header("Section titles")]
        public HudRowView[] sectionRows;

        [Header("Storage upgrades")]
        public Text storageInfoText;
        public HudRowView[] storageRows;

        [Header("Butcher upgrades")]
        public HudRowView[] butcherRows;

        [Header("Bakery upgrades")]
        public HudRowView[] bakeryRows;

        [Header("Deli upgrades")]
        public HudRowView[] deliRows;

        [Header("Alcohol prerequisites and upgrades")]
        public Text licenseStatusText;
        public HudRowView licenseStage1Row;
        public HudRowView licenseStage2Row;
        public HudRowView alcoholSectionPurchaseRow;
        public Text alcoholEmptyText;
        public HudRowView[] alcoholFixtureRows;
    }
}
