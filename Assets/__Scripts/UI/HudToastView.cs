using UnityEngine;
using UnityEngine.UI;

namespace Supermarket.UI
{
    /// <summary>Display adapter for one authored notice toast.</summary>
    public class HudToastView : MonoBehaviour
    {
        public Text message;
        public Image background;
        public CanvasGroup group;
    }
}
