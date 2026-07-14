using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Ensures the scene has the components needed for IPointerClickHandler
/// to work on 2D sprites: Physics2DRaycaster on the camera, and an EventSystem.
/// Attach this to the Main Camera (it runs once in Awake).
/// </summary>
[DefaultExecutionOrder(-100)]
public class TouchInputSetup : MonoBehaviour
{
    void Awake()
    {
        // Ensure Physics2DRaycaster is on the camera
        if (GetComponent<Physics2DRaycaster>() == null)
            gameObject.AddComponent<Physics2DRaycaster>();

        // Ensure an EventSystem exists in the scene
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }
}
