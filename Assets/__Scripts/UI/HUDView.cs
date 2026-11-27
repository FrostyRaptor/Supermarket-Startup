using UnityEngine;
using UnityEngine.UI;

namespace Supermarket.UI
{
    /// <summary>
    /// Serialized references into the authored Tycoon HUD Canvas prefab.
    /// This is a display adapter only; it holds no game logic.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        public Text dayText;
        public Button storeButton;
        public RectTransform noticesContainer;

        public GameObject modalRoot;
        public Button modalCloseButton;
        public Button employeesTab;
        public Button upgradesTab;
        public Button ordersTab;
        public RectTransform modalContent;
        public ScrollRect modalScroll;

        public GameObject employeesPanel;
        public GameObject upgradesPanel;
        public GameObject ordersPanel;

        public HudEmployeesView employees;
        public HudUpgradesView upgrades;
        public HudOrdersView orders;

        public GameObject toastTemplate;
    }
}
