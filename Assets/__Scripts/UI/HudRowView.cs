using UnityEngine;
using UnityEngine.UI;

namespace Supermarket.UI
{
    /// <summary>Display adapter for one authored store-manager row, static or template.</summary>
    public class HudRowView : MonoBehaviour
    {
        public Text label;
        public Text priceText;
        public Button leftButton;
        public Text leftLabel;
        public Button rightButton;
        public Text rightLabel;
        public Image background;
    }
}
