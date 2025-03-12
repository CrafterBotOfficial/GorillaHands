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

    private bool anchored;

#if DEBUG
    private Transform targetPosition_DebugSphere;
#endif 

    private void Start()
    {
        realHand = IsLeft ? GorillaLocomotion.Player.Instance.leftControllerTransform : GorillaLocomotion.Player.Instance.rightControllerTransform;
        inputDevice = IsLeft ? UnityEngine.XR.XRNode.LeftHand : UnityEngine.XR.XRNode.RightHand;

        follower = GameObject.Instantiate(Main.HandPrefab as GameObject).transform;
        followerRigidbody = follower.AddComponent<Rigidbody>();
        followerRigidbody.useGravity = false;

#if DEBUG
        Main.Log("Debug enabled, creating debug objects", BepInEx.Logging.LogLevel.Message);
        // targetPosition_DebugSphere = CreateDebugSphere(Color.white);
#endif
    }

    private void FixedUpdate()
    {
        if (ControllerInputPoller.GetGrab(inputDevice) && IsTouchingTerrain())
        {
            anchored = true;
            followerRigidbody.velocity = Vector3.zero;
            Vector3 basePoint = GorillaLocomotion.Player.Instance.bodyCollider.transform.position;
            Vector3 direction = basePoint - realHand.position;
            Vector3 x = direction * Configuration.ArmOffsetMultiplier.Value + follower.position;

            GorillaLocomotion.Player.Instance.playerRigidBody.velocity = x - GorillaLocomotion.Player.Instance.playerRigidBody.position;
            return;
        }
        if (anchored && ControllerInputPoller.GetGrabRelease(inputDevice))
        {
            anchored = false;
            GorillaLocomotion.Player.Instance.playerRigidBody.velocity *= Configuration.VelocityMultiplierOnRelease.Value;
        }


        targetPosition = CalcTargetPosition();
        followerRigidbody.velocity = (targetPosition - follower.position) * Configuration.ArmOffsetMultiplier.Value; // <-------------------------- prob shouldnt be config, but whatever
        follower.rotation = Quaternion.Lerp(follower.rotation, realHand.rotation.normalized, 0.1f);

        if (Vector3.Distance(follower.position, targetPosition) > 35) follower.position = targetPosition;

#if DEBUG
        // targetPosition_DebugSphere.position = targetPosition;
#endif
    }

    private Vector3 CalcTargetPosition()
    {
        Vector3 playerPosition = GorillaLocomotion.Player.Instance.bodyCollider.transform.position - new Vector3(0, 0.05f, 0); // todo: edit offset
        Vector3 playerToRealHandDirection = realHand.position - playerPosition;
        // float handToPlayerDistance = Vector3.Distance(playerPosition, realHand.position);
        return playerPosition + playerToRealHandDirection * (Configuration.ArmOffsetMultiplier.Value);
    }

    private bool IsTouchingTerrain()
    {
        Collider[] hits = Physics.OverlapSphere(follower.position, 0.5f, 9);
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
    private Transform CreateDebugSphere(Color color, bool actuallyMakeItACube = false, bool removeCollider = true)
    {
        var sphere = GameObject.CreatePrimitive(actuallyMakeItACube ? PrimitiveType.Cube : PrimitiveType.Sphere).transform;
        sphere.localScale *= 0.5f;
        // sphere.gameObject.layer = 10;
        if (removeCollider) Destroy(sphere.GetComponent<Collider>());

        var material = sphere.GetComponent<Renderer>().material;
        material.shader = Shader.Find("GorillaTag/UberShader");
        material.color = color;

        return sphere;
    }
#endif
}