/*
 * Todo: Ensure if hands are closed all movement affects stop
 */

using GorillaLocomotion;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaHands;

public class HandController : MonoBehaviour
{
    public bool IsLeft;
    private XRNode inputDevice;

    private Transform playerHand; // the players actual hand || todo: determine whether the players controller pos or hand pos works better
    private Transform follower; // the visible hand
    private Rigidbody followerRigidbody;
    private Collider? followerCollider;

    private GameObject handGeometry; // The skin mesh object
    private Animator animator;

    private bool anchored;
    private Vector3 anchorPoint;
    private int layerMask = LayerMask.GetMask("Default", "Gorilla Object");

    private bool phasedIn = false;
    private bool primaryButtonPressed = false;
    private float phaseLerp = 0f;

    private const float
        followForceMultiplier = 50f,
        dampingForceMultiplier = 8f,
        maxSnapDistance = 35f,
        rotationLerpSpeed = 0.1f,
        phaseSpeed = 5f;

#if DEBUG
    private Transform targetPosition_DebugSphere;
    private LineRenderer lineRenderer_Debug;
#endif 

    public void Start()
    {
        playerHand = IsLeft
            ? VRRigCache.Instance.localRig.transform.Find("RigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L")
            : VRRigCache.Instance.localRig.transform.Find("RigAnchor/rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R");

        inputDevice = IsLeft
            ? UnityEngine.XR.XRNode.LeftHand
            : UnityEngine.XR.XRNode.RightHand;

        follower = IsLeft
            ? Instantiate(Main.leftHandPrefab as GameObject).transform
            : Instantiate(Main.rightHandPrefab as GameObject).transform;

        handGeometry = IsLeft
            ? follower.Find("hands:hands_geom/hands:Lhand").gameObject
            : follower.Find("hands:hands_geom/hands:Rhand").gameObject;

        VRRig.LocalRig.OnColorChanged += (args) => UpdateColor();
        UpdateColor();

        animator = follower.GetComponent<Animator>();
        follower.transform.localScale = Vector3.one * 8;

        followerRigidbody = follower.AddComponent<Rigidbody>();
        followerRigidbody.useGravity = false;

        if (Configuration.HandCollisions.Value)
            SetupColliders();

#if DEBUG
        Main.Log("Debug enabled, creating debug objects", BepInEx.Logging.LogLevel.Message);
        targetPosition_DebugSphere = CreateDebugSphere(Color.white, removeCollider: true);
        lineRenderer_Debug = new GameObject("raytester9000").AddComponent<LineRenderer>();
        lineRenderer_Debug.startColor = Color.red;
        lineRenderer_Debug.endColor = Color.blue;
        lineRenderer_Debug.material.shader = Shader.Find("GorillaTag/UberShader");
        lineRenderer_Debug.widthCurve = AnimationCurve.Constant(1, 1, .1f);
#endif
    }

    public void FixedUpdate()
    {
#if DEBUG
        targetPosition_DebugSphere.position = CalcTargetPosition();
#endif

        float gripValue = ControllerInputPoller.GripFloat(inputDevice);
        animator.SetFloat("Grip", gripValue);

        HandleHandToggle();

        UpdatePhaseLerp();

        // Moves hand to correct pos
        if (phaseLerp > 0f)
        {
            Vector3 phaseTarget = Vector3.Lerp(CalcTargetPosition(), playerHand.position, phaseLerp);
            follower.position = phaseTarget;

            Vector3 handRotationOffset = IsLeft ? new Vector3(-90, 180, 90) : new Vector3(-90, 180, -90);
            Quaternion targetRotation = playerHand.rotation * Quaternion.Euler(handRotationOffset);
            follower.rotation = Quaternion.Lerp(follower.rotation, targetRotation.normalized, phaseLerp);

            followerRigidbody.velocity = Vector3.zero;

            if (phaseLerp >= 1f)
                return;
        }

        if (ControllerInputPoller.GetGrab(inputDevice))
        {
            bool touchingTerrain = IsTouchingTerrain();
            if (!touchingTerrain && TryRaycastToTerrain(out Vector3 hitPoint))
            {
                if (!anchored) // Incase this is the first frame which the player grips, after this it will be ture
                    anchorPoint = hitPoint;
                AnchorHandAt(anchorPoint);
                ApplyClimbForceToPlayer();
                return;
            }
            else if (touchingTerrain)
            {
                AnchorHandAt(follower.position);
                ApplyClimbForceToPlayer();
                return;
            }
        }

        if (anchored && ControllerInputPoller.GetGrabRelease(inputDevice))
        {
            anchored = false;
            SetCollidersActive(true);
            GTPlayer.Instance.playerRigidBody.velocity *= Configuration.VelocityMultiplierOnRelease.Value; // Release multiplier
        }

        // Todo: add player speed to hand speed to ensure they dont get left behind when moving fast
        Vector3 target = CalcTargetPosition();
        Vector3 offset = target - follower.position;
        Vector3 force = offset * followForceMultiplier - followerRigidbody.velocity * dampingForceMultiplier;

        followerRigidbody.AddForce(force, ForceMode.Acceleration);

        // Follower rotation handler
        Vector3 rotationOffset = IsLeft ? new Vector3(-90, 180, 90) : new Vector3(-90, 180, -90);
        Quaternion desiredRotation = playerHand.rotation * Quaternion.Euler(rotationOffset);
        followerRigidbody.freezeRotation = true;
        follower.rotation = Quaternion.Lerp(follower.rotation, desiredRotation.normalized, rotationLerpSpeed);

        // iirc this was intended to stop the hands from getting stuck, should be moved up
        if (Vector3.Distance(follower.position, target) > maxSnapDistance)
            follower.position = target;
    }

    private void AnchorHandAt(Vector3 position)
    {
        anchored = true;
        SetCollidersActive(false); // Stops hand from randomly rotating when anchre
        follower.position = position;
        followerRigidbody.velocity = Vector3.zero;
        followerRigidbody.angularVelocity = Vector3.zero;
    }

    // Todo: Chin make meshcollider in prefab so we can skip all dis
    // (done?) Todo: fix rotation getting all outda wak when collider touches something or anchored
    private void SetupColliders()
    {
#if DEBUG
        followerCollider = CreateDebugSphere(Color.white, true, false, true).GetComponent<Collider>();//GameObject.CreatePrimitive(PrimitiveType.Cube);
#else
        followerCollider = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Collider>();
#endif
        followerCollider.transform.SetParent(follower, false);
        followerCollider.transform.localScale = new Vector3(.12f, .12f / 2.5f, .12f); // 1/8=.125, hands are initially scaled by 8
        followerCollider.transform.localPosition = IsLeft ? new Vector3(-.02f, .045f, 0) : new Vector3(.02f, .045f, 0);
        followerCollider.includeLayers = layerMask;
    }

    // todo: rename
    private void SetCollidersActive(bool value) {
        if (followerCollider is null) return;
        // Handle rigidbody prepping for anchroing
        followerRigidbody.isKinematic = !value;
    }

    private void HandleHandToggle()
    {
        bool buttonPressed = ControllerInputPoller.PrimaryButtonPress(inputDevice);
        if (buttonPressed && !primaryButtonPressed)
            phasedIn = !phasedIn;
        primaryButtonPressed = buttonPressed;
    }

    private void UpdatePhaseLerp()
    {
        float target = phasedIn ? 1f : 0f;

        phaseLerp = Mathf.MoveTowards(phaseLerp, target, Time.fixedDeltaTime * (phaseSpeed * 0.6f));

        float scale = Mathf.Lerp(8f, 0f, phaseLerp);
        follower.localScale = Vector3.one * scale;
    }

    private bool TryRaycastToTerrain(out Vector3 hitPoint)
    {
        var direction = -follower.up; // from palm
        float distance = .5f; // Todo: make configurable

        Ray ray = new Ray(follower.position, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, distance, layerMask))
        {
            hitPoint = hit.point;
#if DEBUG
            lineRenderer_Debug.SetPositions([ray.origin, hit.point]);
#endif
            return true;
        }

        hitPoint = Vector3.zero;
        return false;
    }
    
    // Todo: only use raycasting for finding terrain to anchor to
    private bool IsTouchingTerrain()
    {
        float radius = 0.15f;
        Collider[] hits = Physics.OverlapSphere(follower.position, radius, layerMask);
        if (followerCollider is not null) return hits.Any(hit => hit.GetComponent<MeshRenderer>() && hit != followerCollider.gameObject);
        else return hits.Any(hit => hit.GetComponent<MeshRenderer>() && hit); 
    }

    private void ApplyClimbForceToPlayer()
    {
        Vector3 basePoint = GTPlayer.Instance.bodyCollider.transform.position;
        Vector3 direction = basePoint - playerHand.position;
        Vector3 targetVelocity = direction * Configuration.ArmOffsetMultiplier.Value + follower.position;
        GTPlayer.Instance.playerRigidBody.velocity = targetVelocity - GTPlayer.Instance.playerRigidBody.position;
    }

    private Vector3 CalcTargetPosition()
    {
        Vector3 playerPosition = GTPlayer.Instance.bodyCollider.transform.position - new Vector3(0, 0.05f, 0);
        Vector3 playerToRealHandDirection = playerHand.position - playerPosition;
        return playerPosition + playerToRealHandDirection * (Configuration.ArmOffsetMultiplier.Value);
    }


    public void UpdateColor()
    {
        if (follower is Transform && handGeometry.GetComponent<SkinnedMeshRenderer>() is SkinnedMeshRenderer renderer)
            renderer.material.color = GorillaTagger.Instance.offlineVRRig.playerColor;
    }

    private void OnEnable()
    {
        follower?.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        follower.gameObject.SetActive(false);
    }

#if DEBUG
    public Transform CreateDebugSphere(Color color, bool actuallyMakeItACube = false, bool removeCollider = true, bool actuallyDontShrinkIt = false)
    {
        var sphere = GameObject.CreatePrimitive(actuallyMakeItACube ? PrimitiveType.Cube : PrimitiveType.Sphere).transform;
        if (!actuallyDontShrinkIt) sphere.localScale *= 0.5f;
        if (removeCollider) Destroy(sphere.GetComponent<Collider>());

        var material = sphere.GetComponent<Renderer>().material;
        material.shader = Shader.Find("GorillaTag/UberShader");
        material.color = color;

        return sphere;
    }
#endif
}
