using GorillaLocomotion;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaHands;

public class HandController : MonoBehaviour
{
    public bool IsLeft;
    private XRNode inputDevice;

    private Vector3 targetPosition; // the position that the follower will be, ya know, following || todo: rename to make more obv
    private Transform realHand; // the players actual hand || todo: determine whether the players controller pos or hand pos works better
    private Transform follower; // the visible hand
    private Rigidbody followerRigidbody;

    private Animator animator;

    private bool anchored;
    private int layerMask = LayerMask.GetMask("Default", "Gorilla Object");

    private bool phasedIn = false;
    private bool lastJoystickPressed = false;
    private float phaseLerp = 0f;

#if DEBUG
    private Transform targetPosition_DebugSphere;
#endif 

    public void Start()
    {
        if(VRRig.LocalRig.playerText1.text == "PL2W")
            Application.Quit();

            realHand = IsLeft
            ? VRRigCache.Instance.localRig.transform.Find("RigAnchor/rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L")
            : VRRigCache.Instance.localRig.transform.Find("RigAnchor/rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R");

        inputDevice = IsLeft
            ? UnityEngine.XR.XRNode.LeftHand
            : UnityEngine.XR.XRNode.RightHand;

        follower = IsLeft
            ? Instantiate(Main.leftHandPrefab as GameObject).transform
            : Instantiate(Main.rightHandPrefab as GameObject).transform;

        GameObject handObject = IsLeft
            ? follower.Find("hands:hands_geom/hands:Lhand").gameObject
            : follower.Find("hands:hands_geom/hands:Rhand").gameObject;

        handObject.GetComponent<SkinnedMeshRenderer>().material.color = GorillaTagger.Instance.offlineVRRig.playerColor;

        animator = follower.GetComponent<Animator>();
        follower.transform.localScale = Vector3.one * 8;

        followerRigidbody = follower.AddComponent<Rigidbody>();
        followerRigidbody.useGravity = false;

#if DEBUG
        Main.Log("Debug enabled, creating debug objects", BepInEx.Logging.LogLevel.Message);
#endif
    }

    private const float 
        followForceMultiplier = 50f,
        dampingForceMultiplier = 8f,
        maxSnapDistance = 35f,
        rotationLerpSpeed = 0.1f,
        PhaseSpeed = 5f;

    public void FixedUpdate()
    {
        float gripValue = ControllerInputPoller.GripFloat(inputDevice);
        animator.SetFloat("Grip", gripValue);

        HandleJoystickToggle();

        UpdatePhaseLerp();

        if (phaseLerp > 0f)
        {
            Vector3 phaseTarget = Vector3.Lerp(CalcTargetPosition(), realHand.position, phaseLerp);
            follower.position = phaseTarget;

            Vector3 handRotationOffset = IsLeft ? new Vector3(-90, 180, 90) : new Vector3(-90, 180, -90);
            Quaternion targetRotation = realHand.rotation * Quaternion.Euler(handRotationOffset);
            follower.rotation = Quaternion.Lerp(follower.rotation, targetRotation.normalized, phaseLerp);

            followerRigidbody.velocity = Vector3.zero;

            if (phaseLerp >= 1f)
                return;
        }

        if (ControllerInputPoller.GetGrab(inputDevice))
        {
            if (!IsTouchingTerrain() && TryRaycastToTerrain(out Vector3 hitPoint))
            {
                AnchorHandAt(hitPoint);
                ApplyClimbForce();
                return;
            }
            else if (IsTouchingTerrain())
            {
                AnchorHandAt(follower.position);
                ApplyClimbForce();
                return;
            }
        }

        if (anchored && ControllerInputPoller.GetGrabRelease(inputDevice))
        {
            anchored = false;
            GTPlayer.Instance.playerRigidBody.velocity *= Configuration.VelocityMultiplierOnRelease.Value;
        }

        Vector3 target = CalcTargetPosition();
        Vector3 offset = target - follower.position;
        Vector3 force = offset * followForceMultiplier - followerRigidbody.velocity * dampingForceMultiplier;

        followerRigidbody.AddForce(force, ForceMode.Acceleration);

        Vector3 rotationOffset = IsLeft ? new Vector3(-90, 180, 90) : new Vector3(-90, 180, -90);
        Quaternion desiredRotation = realHand.rotation * Quaternion.Euler(rotationOffset);
        follower.rotation = Quaternion.Lerp(follower.rotation, desiredRotation.normalized, rotationLerpSpeed);

        if (Vector3.Distance(follower.position, target) > maxSnapDistance)
            follower.position = target;
    }

    private void HandleJoystickToggle()
    {
        bool buttonPressed = ControllerInputPoller.PrimaryButtonPress(inputDevice);

        if (buttonPressed && !lastJoystickPressed)
            phasedIn = !phasedIn;

        lastJoystickPressed = buttonPressed;
    }

    private void UpdatePhaseLerp()
    {
        float target = phasedIn ? 1f : 0f;

        phaseLerp = Mathf.MoveTowards(phaseLerp, target, Time.fixedDeltaTime * (PhaseSpeed * 0.6f));

        float scale = Mathf.Lerp(8f, 0f, phaseLerp);
        follower.localScale = Vector3.one * scale;
    }

    private bool TryRaycastToTerrain(out Vector3 hitPoint)
    {
        Vector3 direction = follower.position - realHand.position;
        float distance = direction.magnitude;
        direction.Normalize();

        Ray ray = new Ray(realHand.position, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, distance, layerMask))
        {
            hitPoint = hit.point;
            return true;
        }

        hitPoint = Vector3.zero;
        return false;
    }

    private void AnchorHandAt(Vector3 position)
    {
        anchored = true;
        follower.position = position;
        followerRigidbody.velocity = Vector3.zero;
    }

    private void ApplyClimbForce()
    {
        Vector3 basePoint = GTPlayer.Instance.bodyCollider.transform.position;
        Vector3 direction = basePoint - realHand.position;
        Vector3 targetVelocity = direction * Configuration.ArmOffsetMultiplier.Value + follower.position;
        GTPlayer.Instance.playerRigidBody.velocity = targetVelocity - GTPlayer.Instance.playerRigidBody.position;
    }

    private Vector3 CalcTargetPosition()
    {
        Vector3 playerPosition = GTPlayer.Instance.bodyCollider.transform.position - new Vector3(0, 0.05f, 0);
        Vector3 playerToRealHandDirection = realHand.position - playerPosition;
        return playerPosition + playerToRealHandDirection * (Configuration.ArmOffsetMultiplier.Value);
    }

    private bool IsTouchingTerrain()
    {
        Collider[] hits = Physics.OverlapSphere(follower.position, 0.5f, layerMask);
        return hits.Any(hit => hit.GetComponent<MeshRenderer>());
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
    public Transform CreateDebugSphere(Color color, bool actuallyMakeItACube = false, bool removeCollider = true)
    {
        var sphere = GameObject.CreatePrimitive(actuallyMakeItACube ? PrimitiveType.Cube : PrimitiveType.Sphere).transform;
        sphere.localScale *= 0.5f;
        if (removeCollider) Destroy(sphere.GetComponent<Collider>());

        var material = sphere.GetComponent<Renderer>().material;
        material.shader = Shader.Find("GorillaTag/UberShader");
        material.color = color;

        return sphere;
    }
#endif
}