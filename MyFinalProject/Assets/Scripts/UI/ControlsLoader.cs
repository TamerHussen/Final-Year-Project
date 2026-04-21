using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class ControlsLoader : MonoBehaviour
{
    private void Start()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput == null) return;

        string json = PlayerPrefs.GetString("InputOverrides", "");
        if (string.IsNullOrEmpty(json)) return;

        playerInput.actions.Disable();
        playerInput.actions.LoadBindingOverridesFromJson(json);
        playerInput.actions.Enable();

        Debug.Log("[ControlsLoader] binding overrides applied to PlayerInput");
    }
}
