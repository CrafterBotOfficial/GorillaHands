using GorillaLocomotion;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaHands;

public class HandController : MonoBehaviour
{
    public bool IsLeft;
    private XRNode inputDevice;

    public Transform PlayerHand;
    public Transform Follower; // The gaint hand that follows the target pos
    public Rigidbody FollowerRigidbody;
    public Collider? FollowerCollider;

    private GameObject handGeometry; // The skin mesh object
    private AnimationManager animationManager;

    private bool anchored;
    private Vector3 anchorPoint;
    public int TerrainLayers = LayerMask.GetMask("Default", "Gorilla Object");

    public Vector3 TargetPosition;

#if DEBUG
    private const string DEBUG_OBJECT_SHADER = "GorillaTag/UberShader";
    private Transform targetPosition_DebugSphere;
    private LineRenderer lineRenderer_HandTouchTerrain_Debug;
#endif 

    public void Start()
    {
        PlayerHand = IsLeft
            ? VRRigCache.Instance.localRig.transform.Find("RigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L")
            : VRRigCache.Instance.localRig.transform.Find("RigAnchor/rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R");

        inputDevice = IsLeft
            ? UnityEngine.XR.XRNode.LeftHand
            : UnityEngine.XR.XRNode.RightHand;

        Follower = IsLeft
            ? Instantiate(Main.leftHandPrefab as GameObject).transform
            : Instantiate(Main.rightHandPrefab as GameObject).transform;

        handGeometry = IsLeft
            ? Follower.Find("hands:hands_geom/hands:Lhand").gameObject
            : Follower.Find("hands:hands_geom/hands:Rhand").gameObject;

        VRRig.LocalRig.OnColorChanged += (args) => UpdateColor();
        UpdateColor();

        // -- Animation setup
        animationManager = this.AddComponent<AnimationManager>();
        animationManager.Controller = this;
        animationManager.InputDevice = inputDevice;

        FollowerRigidbody = Follower.AddComponent<Rigidbody>();
        FollowerRigidbody.freezeRotation = true;
        FollowerRigidbody.useGravity = false;

        if (Configuration.HandCollisions.Value)
            SetupColliders();

#if DEBUG
        Main.Log("Debug enabled, creating debug objects", BepInEx.Logging.LogLevel.Message);
        targetPosition_DebugSphere = CreateDebugSphere(Color.white, removeCollider: true);
        lineRenderer_HandTouchTerrain_Debug = CreateDebugLine(Configuration.HandSpherecastRadius.Value);
#endif
    }

    public void FixedUpdate()
    {
        TargetPosition = CalculateTargetPosition();

#if DEBUG
        targetPosition_DebugSphere.position = TargetPosition;
#endif

        // Hand open/close transitions/animations are handled by IsAnimating
        if (animationManager.IsAnimating() || animationManager.HandHidden()) // Todo: I dislike having the handhidden check within the transition manager. Hand state should be manged here
            return;

        // todo: test in breach map to ensure this isnt annoying as hell, since it will kick you out of climb state if
        // you grab something else
        bool grabbingAllowed = !GTPlayer.Instance.isClimbing; // todo: add any other dangerous interactions
        if (ControllerInputPoller.GetGrab(inputDevice) && grabbingAllowed)
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
            else if (touchingTerrain) // mainly for when colliders are disabled, so we can prob get rid of this
            {
                AnchorHandAt(Follower.position);
                ApplyClimbForceToPlayer();
                return;
            }
        }

        if (anchored)
        {
            anchored = false;
            SetCollidersActive(true);
            GTPlayer.Instance.playerRigidBody.velocity *= Configuration.VelocityMultiplierOnRelease.Value; // Release multiplier
        }

        // If the hand gets stuck, free it
        // Todo: Investigate having a mixed system where the hand's collider cant temp turn off to reach the target, but then if it gets to fare it tps
        if (Vector3.Distance(Follower.position, TargetPosition) > Configuration.HandStuckDistanceThreshold.Value)
            Follower.position = TargetPosition;

        float playerSpeed = GTPlayer.Instance.RigidbodyVelocity.magnitude;
        Vector3 offset = TargetPosition - Follower.position;
        Vector3 force = offset * (Configuration.FollowForceMultiplier.Value + playerSpeed * 10) - FollowerRigidbody.velocity * Configuration.DampingForceMultiplier.Value;

        FollowerRigidbody.AddForce(force, ForceMode.Acceleration);

        ApplyRotationaryForce();
    }

    private void AnchorHandAt(Vector3 position)
    {
        if (!anchored)
        {
            SetCollidersActive(false); // Stops hand from randomly rotating when anchre
        }
        anchored = true;
        Follower.position = position;
        FollowerRigidbody.velocity = Vector3.zero;
        // FollowerRigidbody.angularVelocity = Vector3.zero;
    }

    public void ApplyRotationaryForce()
    {
        Vector3 rotationOffset = IsLeft ? new Vector3(-90, 180, 90) : new Vector3(-90, 180, -90);
        Quaternion desiredRotation = PlayerHand.rotation * Quaternion.Euler(rotationOffset);
        Follower.rotation = Quaternion.Lerp(Follower.rotation, desiredRotation.normalized, Configuration.RotationLerpAmount.Value);
    }

    // Todo: Chin make meshcollider in prefab so we can skip all dis
    // (done?) Todo: fix rotation getting all outda wak when collider touches something or anchored
    // Todo: Test release build to ensure colliders FEEL correct
    private void SetupColliders()
    {
#if DEBUG
        FollowerCollider = CreateDebugSphere(Color.white, true, false, true).GetComponent<Collider>();//GameObject.CreatePrimitive(PrimitiveType.Cube);
#else
        FollowerCollider = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Collider>();
#endif
        FollowerCollider.transform.SetParent(Follower, false);
        FollowerCollider.transform.localScale = new Vector3(.12f, .12f / 2.5f, .12f); // 1/8=.125, hands are initially scaled by 8
        FollowerCollider.transform.localPosition = IsLeft ? new Vector3(-.02f, .045f, 0) : new Vector3(.02f, .045f, 0);
        FollowerCollider.includeLayers = TerrainLayers;
    }

    // todo: rename
    private void SetCollidersActive(bool value)
    {
        if (FollowerCollider is null) return;
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
        if (FollowerCollider is not null) return hits.Any(hit => hit.GetComponent<MeshRenderer>() && hit != FollowerCollider.gameObject);
        else return hits.Any(hit => hit.GetComponent<MeshRenderer>() && hit);
    }

    private void ApplyClimbForceToPlayer()
    {
        Vector3 basePoint = GTPlayer.Instance.bodyCollider.transform.position;
        Vector3 direction = basePoint - PlayerHand.position;
        Vector3 targetVelocity = direction * Configuration.ArmOffsetMultiplier.Value + Follower.position;
        GTPlayer.Instance.playerRigidBody.velocity = targetVelocity - GTPlayer.Instance.playerRigidBody.position;
    }

    private Vector3 CalculateTargetPosition()
    {
        Vector3 playerPosition = GTPlayer.Instance.bodyCollider.transform.position - new Vector3(0, 0.05f, 0);
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
    private void OnDisable() { Follower.gameObject.SetActive(false); }

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
