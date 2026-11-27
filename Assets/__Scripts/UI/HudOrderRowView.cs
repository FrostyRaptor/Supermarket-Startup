using UnityEngine;
using UnityEngine.UI;

namespace Supermarket.UI
{
    /// <summary>Display adapter for one authored product-order row.</summary>
    public class HudOrderRowView : MonoBehaviour
    {
        public Text nameText;
        public Text costText;
        public Text suggestionText;
        public InputField quantityInput;
        public Button orderButton;
        public Graphic background;
    }
}
