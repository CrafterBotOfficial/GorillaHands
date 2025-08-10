// State manager should be moved to the hand class
using UnityEngine;
using UnityEngine.XR;

namespace GorillaHands;

public class AnimationManager : MonoBehaviour
{
    public HandController Controller;
    public XRNode InputDevice;

    private HandState handState;
    private float transitionT;
    private Vector3 targetPosition;

    private Animator animator;

#if DEBUG
    private LineRenderer lineRenderer_PlayerHandToTargetPos_Debug;
#endif

    //     public AnimationManager(HandController controller, XRNode inputDevice)
    //     {
    //         Controller = controller;
    //         InputDevice = inputDevice;
    //         handState = HandState.Closed;
    // #if DEBUG
    //         lineRenderer_PlayerHandToTargetPos_Debug = Controller.CreateDebugLine(.1f);
    // #endif
    //     }
    //
    private void Start()
    {
        animator = Controller.Follower.GetComponent<Animator>();
        Controller.Follower.transform.localScale = Vector3.one * 8;
#if DEBUG
        lineRenderer_PlayerHandToTargetPos_Debug = Controller.CreateDebugLine(.1f);
#endif
    }

    public bool HandHidden() => handState == HandState.Closed;

    public void Update()
    {
        float gripValue = ControllerInputPoller.GripFloat(InputDevice);
        animator.SetFloat("Grip", gripValue);
    }

    public bool IsAnimating()
    {
        bool buttonPressed = ControllerInputPoller.PrimaryButtonPress(InputDevice);
        bool inTransition = handState == HandState.Opening || handState == HandState.Closing;

        if (inTransition)
        {
            HandleHandTransition();
            return true;
        }

        if (buttonPressed)
        {
            handState = handState == HandState.Open ? HandState.Closing : HandState.Opening;
            Main.Log($"Setting hand state to {handState}");
            if (handState == HandState.Opening)
            {
                transitionT = 1; // I'd prefer just use an animator as this feels tacky
                targetPosition = GetHandSpawnPoint();
            }
            else targetPosition = Controller.TargetPosition;
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
        transitionT = Mathf.MoveTowards(transitionT, scaleTarget, Time.fixedDeltaTime * (Configuration.TransitionSpeed.Value * 0.6f)); // Chin, what is 0.6 from?
        float scale = Mathf.Lerp(8f, 0f, transitionT);
        Controller.Follower.localScale = Vector3.one * scale;

        // todo: hands should phase in (close) from there actual position, not the target pos. (when the hands get stuck it looks ugly)
        // hand movement
        Vector3 phaseTarget = Vector3.Lerp(targetPosition, Controller.PlayerHand.position, transitionT);
        Controller.Follower.position = phaseTarget;
        Controller.FollowerRigidbody.velocity = Vector3.zero;

        // hand rot
        Controller.ApplyRotationaryForce();

        if (AnimationComplete())
        {
            Main.Log("Completed animation, or sorry transition");
            handState = handState == HandState.Opening ? HandState.Open : HandState.Closed;
        }
    }

    private bool AnimationComplete()
    {
        if (handState == HandState.Opening)
            return transitionT <= 0;
        if (handState == HandState.Closing)
            return transitionT >= 1;
        Main.Log("What is even going on anymore?");
        return false;
    }

    // Todo: When hands spawn to close to player (like if the player ius pointing straight down) the player will be flung as the hands will clip into him
    private Vector3 GetHandSpawnPoint()
    {
        Vector3 direction = Controller.TargetPosition - Controller.PlayerHand.position;
        float distance = Vector3.Distance(Controller.PlayerHand.position, Controller.TargetPosition);
        Ray ray = new Ray(Controller.PlayerHand.position, direction);

        if (!Physics.Raycast(Controller.PlayerHand.position, direction, out RaycastHit hit, distance, Controller.TerrainLayers))
            return Controller.TargetPosition;

#if DEBUG
        lineRenderer_PlayerHandToTargetPos_Debug.SetPositions([Controller.PlayerHand.position, hit.point]);
#endif
        // moves hand closer to avoid clipping into terrain
        Vector3 unit = direction.normalized;
        Vector3 spawn = hit.point - unit;
        return spawn;
    }

    private enum HandState
    {
        Opening,
        Open,
        Closing,
        Closed
    }
}
