using System;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaHands;

public class AnimationManager
{
    public HandController Controller;

    public float OpenHandScale => 8f * Configuration.HandScaleMultiplier.Value;

    private InputManager inputManager;
    private const float debouncePeriodThreshold = .5f;
    private float debounceTime;

    private float transitionT;
    private Vector3 targetPosition;

    private Animator animator;

#if DEBUG
    private LineRenderer lineRenderer_PlayerHandToTargetPos_Debug;
#endif

    public AnimationManager(HandController controller, XRNode inputDevice, Animator gripAnimator)
    {
        Controller = controller;
        inputManager = new InputManager(inputDevice);
        animator = gripAnimator;
        Controller.Follower.transform.localScale = Vector3.one * 8;

#if DEBUG
        lineRenderer_PlayerHandToTargetPos_Debug = Controller.CreateDebugLine(.1f);
#endif
    }

    public bool IsAnimating()
    {
        bool inTransition = Controller.HandState == HandState.Opening || Controller.HandState == HandState.Closing;

        float gripValue = ControllerInputPoller.GripFloat(inputManager.Node); // todo: move to inputmanager for consistancy (also make it not change the grip value if nothings happened)
        animator.SetFloat("Grip", gripValue);

        if (inTransition)
        {
            HandleHandTransition();
            return true;
        }

        bool buttonPressed = inputManager.GetButtonPress();
        bool debounced = Time.time > debounceTime;
        if (buttonPressed && debounced)
        {
            debounceTime = Time.time + debouncePeriodThreshold;
            Controller.HandState = Controller.HandState == HandState.Open ? HandState.Closing : HandState.Opening;
            Main.Log($"Setting hand state to {Controller.HandState}");
            if (Controller.HandState == HandState.Opening)
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
        float scaleTarget = Controller.HandState == HandState.Opening ? 0f : 1f;
        transitionT = Mathf.MoveTowards(transitionT, scaleTarget, Time.fixedDeltaTime * (Configuration.TransitionSpeed.Value * 0.6f));
        float scale = Mathf.Lerp(OpenHandScale, 0f, transitionT);
        Controller.Follower.localScale = Vector3.one * scale;

        // todo: hands should phase in (close) from there actual position, not the target pos. (when the hands get stuck it looks ugly)
        // hand movement
        Vector3 phaseTarget = Vector3.Lerp(targetPosition, Controller.PlayerHand.position, transitionT);
        Controller.Follower.position = phaseTarget;
        Controller.FollowerRigidbody.linearVelocity = Vector3.zero;

        Controller.ApplyRotationaryForce();

        if (AnimationComplete())
        {
            Main.Log("Completed animation");
            Controller.HandState = Controller.HandState == HandState.Opening ? HandState.Open : HandState.Closed;
        }
    }

    private bool AnimationComplete() =>
        Controller.HandState switch
        {
            HandState.Opening => transitionT <= 0,
            HandState.Closing => transitionT >= 1,
            _ => throw new InvalidOperationException("Bad hand state"),
        };

    // Todo: When hands spawn to close to player (like if the player ius pointing straight down) the player will be flung as the hands will clip into him
    private Vector3 GetHandSpawnPoint()
    {
        Vector3 direction = Controller.TargetPosition - Controller.PlayerHand.position;
        float distance = Vector3.Distance(Controller.PlayerHand.position, Controller.TargetPosition);
        var ray = new Ray(Controller.PlayerHand.position, direction);

        if (!Physics.Raycast(Controller.PlayerHand.position, direction, out RaycastHit hit, distance, Controller.TerrainLayers))
            return Controller.TargetPosition;

#if DEBUG
        lineRenderer_PlayerHandToTargetPos_Debug.SetPositions([Controller.PlayerHand.position, hit.point]);
#endif
        // moves hand closer to the player to avoid clipping into terrain
        Vector3 unit = direction.normalized;
        Vector3 spawn = hit.point - unit;
        return spawn;
    }
}
