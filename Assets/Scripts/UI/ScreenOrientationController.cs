using UnityEngine;

namespace Chkobba
{
    /// <summary>
    /// Per-scene screen orientation. MainMenu = portrait, GameScene = landscape.
    /// Attach one instance per scene and set the desired orientation in the inspector.
    /// Applies on scene load (awake) and on focus regain (mobile app switch / multitasking).
    /// </summary>
    public class ScreenOrientationController : MonoBehaviour
    {
        [Tooltip("Portrait for menu screens, Landscape for gameplay.")]
        public ScreenOrientation orientation = ScreenOrientation.Portrait;

        void Awake() { Apply(); }
        void OnApplicationFocus(bool hasFocus) { if (hasFocus) Apply(); }

        public void Apply()
        {
            if (Screen.orientation != orientation)
                Screen.orientation = orientation;
            // While the OS rotates, refuse the other axis so the layout never sits mid-swap.
            if (orientation == ScreenOrientation.Portrait)
                Screen.autorotateToLandscapeLeft = false;
            else
                Screen.autorotateToPortrait = false;
        }
    }
}
