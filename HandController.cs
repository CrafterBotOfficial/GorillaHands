using System;
using GorillaLocomotion;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaHands;

public class HandController : MonoBehaviour
{
    public bool IsLeft;
    private XRNode inputDevice;

    public HandState HandState;

    public Transform PlayerHand;
    public Transform Follower; // The gaint hand that follows the target pos
    public Rigidbody FollowerRigidbody;
    public Collider FollowerCollider;

    private GameObject handGeometry; // The skin mesh object
    private AnimationManager animationManager;

    private HandStuckManager handStuckManager;

    private bool anchored;
    private Vector3 anchorPoint;
    public int TerrainLayers = LayerMask.GetMask("Default", "Gorilla Object");

    public Vector3 TargetPosition;

    private const string DEBUG_OBJECT_SHADER = "GorillaTag/UberShader";
#if DEBUG
    private Transform targetPosition_DebugSphere;
    private LineRenderer lineRenderer_HandTouchTerrain_Debug;
#endif 

    public void Start()
    {
        PlayerHand = IsLeft
            ? VRRigCache.Instance.localRig.transform.Find("GorillaPlayerNetworkedRigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L")
            : VRRigCache.Instance.localRig.transform.Find("GorillaPlayerNetworkedRigAnchor/rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R");

        inputDevice = IsLeft
            ? UnityEngine.XR.XRNode.LeftHand
            : UnityEngine.XR.XRNode.RightHand;

        Follower = IsLeft
            ? Instantiate(Main.leftHandPrefab as GameObject).transform
            : Instantiate(Main.rightHandPrefab as GameObject).transform;

        handGeometry = IsLeft
            ? Follower.Find("hands:hands_geom/hands:Lhand").gameObject
            : Follower.Find("hands:hands_geom/hands:Rhand").gameObject;

        VRRig.LocalRig.OnColorChanged += _ => UpdateColor();
        UpdateColor();

        FollowerRigidbody = Follower.AddComponent<Rigidbody>();
        FollowerRigidbody.freezeRotation = true;
        FollowerRigidbody.useGravity = false;

        // -- Animation setup
        animationManager = new AnimationManager(this, inputDevice, Follower.GetComponent<Animator>());

        handStuckManager = new HandStuckManager(this, FollowerRigidbody);

        if (Configuration.HandCollisions.Value)
            SetupColliders();

        HandState = HandState.Closed;

#if DEBUG
        Main.Log("Debug enabled, creating debug objects", BepInEx.Logging.LogLevel.Message);
        targetPosition_DebugSphere = CreateDebugSphere(Color.white, removeCollider: true);
        lineRenderer_HandTouchTerrain_Debug = CreateDebugLine(Configuration.HandSpherecastRadius.Value);

        // var offsetDebug2 = CreateDebugSphere(Color.red, removeCollider: true).transform;
        // offsetDebug2.SetParent(VRRig.LocalRig.transform);
        // offsetDebug2.localPosition = new Vector3(.05f, -.2f, 0);
        // var offsetDebug = CreateDebugSphere(Color.white, removeCollider: true).transform;
        // offsetDebug.SetParent(GTPlayer.Instance.bodyCollider.transform);
        // offsetDebug.localPosition = new Vector3(0, -0.05f, 0);
#endif
    }

    public void FixedUpdate()
    {
        TargetPosition = CalculateTargetPosition();

#if DEBUG
        targetPosition_DebugSphere.position = TargetPosition;
#endif

        // Hand open/close transitions/animations are handled by IsAnimating
        if (animationManager.IsAnimating() || HandHidden())
            return;

        bool grabbingAllowed = !GTPlayer.Instance.isClimbing; // todo: add any other dangerous interactions
        if (ControllerInputPoller.GetGrab(inputDevice) && grabbingAllowed) // todo: move to InputManager for consistancy
        {
            bool touchingTerrain = IsTouchingTerrain();
            if (!touchingTerrain && TryRaycastToTerrain(out Vector3 hitPoint))
            {
                if (!anchored) anchorPoint = hitPoint;
                AnchorHandAt(anchorPoint);
                ApplyClimbForceToPlayer();
                return;
            }
            else if (touchingTerrain) // mainly for when colliders are disabled, so we can prob get rid of this since nobody will use the mod with no colliders
            {
                AnchorHandAt(Follower.position);
                ApplyClimbForceToPlayer();
                return;
            }
        }

        if (anchored)
        {
            anchored = false;
            FreezeRigidbody(true);
            GTPlayer.Instance.playerRigidBody.linearVelocity *= Configuration.VelocityMultiplierOnRelease.Value; // Release multiplier
        }
        else handStuckManager.CheckHandFreedom();

        float playerSpeed = GTPlayer.Instance.RigidbodyVelocity.magnitude;
        Vector3 offset = TargetPosition - Follower.position;
        Vector3 force = offset * (Configuration.FollowForceMultiplier.Value + playerSpeed * 10) - FollowerRigidbody.linearVelocity * Configuration.DampingForceMultiplier.Value;

        FollowerRigidbody.AddForce(force, ForceMode.Acceleration);

        ApplyRotationaryForce();
    }

    private void AnchorHandAt(Vector3 position)
    {
        if (!anchored)
        {
            FreezeRigidbody(false);
        }
        anchored = true;
        Follower.position = position;
        // FollowerRigidbody.linearVelocity = Vector3.zero;
        // FollowerRigidbody.angularVelocity = Vector3.zero;
    }

    public void ApplyRotationaryForce()
    {
        Vector3 rotationOffset = IsLeft ? new Vector3(-90, 180, 90) : new Vector3(-90, 180, -90);
        Quaternion desiredRotation = PlayerHand.rotation * Quaternion.Euler(rotationOffset);
        Follower.rotation = Quaternion.Lerp(Follower.rotation, desiredRotation.normalized, Configuration.RotationLerpAmount.Value);
    }

    private void SetupColliders()
    {
#if DEBUG
        FollowerCollider = CreateDebugSphere(Color.white, true, false, true).GetComponent<Collider>();//GameObject.CreatePrimitive(PrimitiveType.Cube);
#else
        FollowerCollider = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Collider>();
        Destroy(FollowerCollider.GetComponent<Renderer>());
#endif

        FollowerCollider.transform.SetParent(Follower, false);
        FollowerCollider.transform.localPosition = IsLeft ? new Vector3(-.02f, .045f, 0) : new Vector3(.02f, .045f, 0); // y:.045
        FollowerCollider.transform.localScale = new Vector3(.095f, .12f / 2.5f, .12f); // 1/8=.125, hands are initially scaled by 8
        FollowerCollider.includeLayers = TerrainLayers;
    }

    private void FreezeRigidbody(bool value)
    {
        if (FollowerCollider == null) return;
        // Handle rigidbody prepping for anchroing
        FollowerRigidbody.isKinematic = !value;
    }

    private bool TryRaycastToTerrain(out Vector3 hitPoint)
    {
        var direction = -Follower.up; // from palm
        const float distance = .5f; // Todo: make configurable

        Ray ray = new Ray(Follower.position, direction);
        if (Physics.SphereCast(ray, Configuration.HandSpherecastRadius.Value, out RaycastHit hit, distance, TerrainLayers))
        {
            hitPoint = hit.point;
#if DEBUG
            lineRenderer_HandTouchTerrain_Debug.SetPositions([ray.origin, hit.point]);
#endif
            return true;
        }

        hitPoint = Vector3.zero;
        return false;
    }

    private bool IsTouchingTerrain()
    {
        float radius = 0.15f;
        Collider[] hits = Physics.OverlapSphere(Follower.position, radius, TerrainLayers);
        return hits.Any(hit => hit && hit.GetComponent<MeshRenderer>());
    }

    private void ApplyClimbForceToPlayer()
    {
        Vector3 basePoint = GTPlayer.Instance.bodyCollider.transform.position;
        Vector3 direction = basePoint - PlayerHand.position;
        Vector3 targetVelocity = direction * Configuration.ArmOffsetMultiplier.Value + Follower.position;
        GTPlayer.Instance.playerRigidBody.linearVelocity = targetVelocity - GTPlayer.Instance.playerRigidBody.position;
    }

    private bool HandHidden()
    {
        return HandState == HandState.Closed || HandState == HandState.Closing;
    }

    private Vector3 CalculateTargetPosition()
    {
        Vector3 playerPosition = VRRig.LocalRig.transform.position + new Vector3(.05f, -.2f, 0);
        Vector3 playerToRealHandDirection = PlayerHand.position - playerPosition;
        return playerPosition + playerToRealHandDirection * (Configuration.ArmOffsetMultiplier.Value);
    }

    // Chin, change this so it uses the player material. Im not sure if the asset will allow for it so its up to you
    public void UpdateColor()
    {
        if (Follower is Transform && handGeometry.GetComponent<SkinnedMeshRenderer>() is SkinnedMeshRenderer renderer)
            renderer.material.color = GorillaTagger.Instance.offlineVRRig.playerColor;
    }

    private void OnEnable() { Follower?.gameObject.SetActive(true); }
    private void OnDisable() { Follower?.gameObject.SetActive(false); }

#if DEBUG
    public LineRenderer CreateDebugLine(float width)
    {
        var line = new GameObject("raytester9000").AddComponent<LineRenderer>();
        line.material.shader = Shader.Find(DEBUG_OBJECT_SHADER);
        line.widthCurve = AnimationCurve.Constant(1, 1, width);
        return line;
    }

    public Transform CreateDebugSphere(Color color, bool actuallyMakeItACube = false, bool removeCollider = true, bool actuallyDontShrinkIt = false)
    {
        var sphere = GameObject.CreatePrimitive(actuallyMakeItACube ? PrimitiveType.Cube : PrimitiveType.Sphere).transform;
        if (!actuallyDontShrinkIt) sphere.localScale *= 0.5f;
        if (removeCollider) Destroy(sphere.GetComponent<Collider>());

        var material = sphere.GetComponent<Renderer>().material;
        material.shader = Shader.Find(DEBUG_OBJECT_SHADER);
        material.color = color;

        return sphere;
    }
#endif
}

public enum HandState
{
    Opening,
    Open,
    Closing,
    Closed
}
