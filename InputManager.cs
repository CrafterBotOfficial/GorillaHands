using System;
using UnityEngine.XR;
using Valve.VR;

namespace GorillaHands;

public class InputManager
{
    public Func<bool> GetButtonPress;
    public XRNode Node;

    public InputManager(XRNode hand)
    {
        GetButtonPress =
            Configuration.ToggleHandVisiblityButton.Value switch
            {
                ControllerButton.Primary => () => ControllerInputPoller.PrimaryButtonPress(hand),
                ControllerButton.Secondary => () => ControllerInputPoller.SecondaryButtonPress(hand),
                ControllerButton.JoystickDown => () => GetJoystickDown(hand),
                _ => () => BadConfigError(hand),
            };
        Node = hand;
    }

    private bool BadConfigError(XRNode hand)
    {
        Main.Log("Bad config file, please delete it", BepInEx.Logging.LogLevel.Warning);
        return ControllerInputPoller.PrimaryButtonPress(hand);
    }

    private bool GetJoystickDown(XRNode hand)
    {
        bool clicked;

        bool steamVr = GorillaNetworking.PlayFabAuthenticator.instance.platform.ToString().ToLower() == "steam";
        if (steamVr)
        {
            clicked = hand == XRNode.LeftHand
                ? SteamVR_Actions.gorillaTag_LeftJoystickClick.state
                : SteamVR_Actions.gorillaTag_RightJoystickClick.state;
            return clicked;
        }

        var device = GetDevice();
        if (!device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out clicked)) // https://docs.unity3d.com/Manual/xr_input.html
        {
            Main.Log("Hand input method not found", BepInEx.Logging.LogLevel.Error);
            return false;
        }

        return clicked;
    }

    private InputDevice GetDevice() =>
        Node == XRNode.LeftHand
            ? ControllerInputPoller.instance.leftControllerDevice
            : ControllerInputPoller.instance.rightControllerDevice;
}
