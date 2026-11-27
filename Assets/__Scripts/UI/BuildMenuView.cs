using UnityEngine;
using UnityEngine.UI;

namespace Supermarket.UI
{
    /// <summary>
    /// Serialized references into the authored Build Menu Canvas prefab.
    /// This is a display adapter only; it holds no game logic.
    /// </summary>
    public class BuildMenuView : MonoBehaviour
    {
        public GameObject buildBar;
        public Text hintsText;
        public Text messageText;
        public CanvasGroup messageGroup;
        public RectTransform hotbarContainer;
        public Button rotateButton;
        public Button closeButton;

        public Text currencyText;
        public Text rateText;

        public Button buildModeButton;
        public Text buildModeLabel;

        public GameObject hotbarItemTemplate;
    }
}
