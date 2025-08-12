using System;
using UnityEngine.XR;

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
                ControllerButton.JoystickDown => () => GetJoystickDown(),
                _ => () => ControllerInputPoller.PrimaryButtonPress(hand), // todo: disable warning in csproj so this line can be removed
            };
        Node = hand;
    }

    // todo: replace these null checks with the controllerinputpoller's valid booleans
    private bool GetJoystickDown()
    {
        if (!ControllerInputPoller.instance)
        {
            Main.Log("Controller poller not found, likely broken from a game update.", BepInEx.Logging.LogLevel.Error);
            return false;
        }

        var device = GetDevice();
        if (device == null)
        {
            Main.Log("hand controller not found!", BepInEx.Logging.LogLevel.Error);
            return false;
        }

        // this doesnt seem to  ork on my device, and it might be because of proton issues. Whatever the case Chin make this work. It must act as a toggle so impliment that logic below - crafterbot
        if (!device.TryGetFeatureValue(CommonUsages.secondary2DAxisTouch, out bool stickClicked)) // https://docs.unity3d.com/Manual/xr_input.html
        {
            Main.Log("Hand input method not found", BepInEx.Logging.LogLevel.Error);
            return false;
        }

        return stickClicked;
    }

    private InputDevice GetDevice() =>
        Node == XRNode.LeftHand
            ? ControllerInputPoller.instance.leftControllerDevice
            : ControllerInputPoller.instance.rightControllerDevice;
}
