using UnityEngine;
using UnityEngine.UI;

namespace Supermarket.UI
{
    /// <summary>Static contents of the authored Orders tab.</summary>
    public class HudOrdersView : MonoBehaviour
    {
        public HudOrderRowView[] rows;
        public Text topTotalText;
        public Button topSubmitButton;
        public Text bottomTotalText;
        public Button bottomSubmitButton;
    }
}
