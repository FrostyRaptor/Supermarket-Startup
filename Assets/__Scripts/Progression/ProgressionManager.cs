using UnityEngine;
using Supermarket.Config;
using Supermarket.Core;
using Supermarket.Store;

namespace Supermarket.Progression
{
    /// <summary>Progression gates: sections unlock by purchase, alcohol needs the license.</summary>
    public class ProgressionManager : MonoBehaviour
    {
        public SectionManager sections;
        public LiquorLicense license;
        public NotificationCenter notices;

        public bool CanSellAlcohol
        {
            get { return license != null && license.IsApproved && sections != null && sections.IsUnlocked(StoreSectionType.Alcohol); }
        }

        public bool TryUnlockSection(StoreSectionType type)
        {
            if (type == StoreSectionType.Alcohol && license != null && !license.IsApproved)
            {
                if (notices != null)
                    notices.Notify("You must obtain a liquor license before buying the alcohol section.", NoticeKind.Warning);
                return false;
            }
            return sections != null && sections.TryUnlock(type);
        }
    }
}
