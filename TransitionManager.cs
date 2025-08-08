using UnityEngine;
using UnityEngine.XR;

namespace GorillaHands;

public class TransitionManager
{
    public HandController Controller;
    public XRNode InputDevice;

    private HandState handState;
    private float transitionT;

    private float 
        rotationLerpSpeed = 0.1f,
        transitionSpeed = 5f;

    public TransitionManager(HandController controller, XRNode inputDevice)
    {
        Controller = controller;
        InputDevice = inputDevice;
        handState = HandState.Closed;
    }

    public bool HandHidden() => handState == HandState.Closed;

    public bool IsAnimating()
    {
        bool buttonPressed = ControllerInputPoller.PrimaryButtonPress(InputDevice);
        bool inTransition = handState == HandState.Opening || handState == HandState.Closing;

        if (inTransition)
        {
            HandleHandTransition();
            return true;
        }

        // Todo: make not ugly - temp code
        if (/*!inTransition &&*/ buttonPressed)
        {
            handState = handState == HandState.Open ? HandState.Closing : HandState.Opening;
            Main.Log($"Setting hand state to {handState}");
            if (handState == HandState.Opening) transitionT = 1; // I'd prefer just use an animator as this feels tacky
            HandleHandTransition();
            return true;
        }

        return false;
    }

    // Phase in and out / transition
    private void HandleHandTransition()
    {
        // hand scale
        float scaleTarget = handState == HandState.Opening ? 0f : 1f;
        transitionT = Mathf.MoveTowards(transitionT, scaleTarget, Time.fixedDeltaTime * (transitionSpeed * 0.6f)); // Chin, what is 0.6 from?
        float scale = Mathf.Lerp(8f, 0f, transitionT);
        Controller.Follower.localScale = Vector3.one * scale;

        // hand movement
        Vector3 phaseTarget = Vector3.Lerp(Controller.TargetPosition, Controller.PlayerHand.position,  transitionT);
        Controller.Follower.position = phaseTarget;
        Controller.FollowerRigidbody.velocity = Vector3.zero;

        // hand rot
        Controller.ApplyRotationaryForce(); 

        if (AnimationComplete()) {
            Main.Log("Completed animation, or sorry transition");
            handState = handState == HandState.Opening ? HandState.Open : HandState.Closed;
        }
    }

    private bool AnimationComplete() {
        if (handState == HandState.Opening) 
            return transitionT <= 0;
        if (handState == HandState.Closing)
            return transitionT >= 1;
        Main.Log("What is even going on anymore?");
        return false;
    }

    private enum HandState
    {
        Opening,
        Open,
        Closing,
        Closed
    }
}
