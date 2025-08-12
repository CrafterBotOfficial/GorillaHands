using UnityEngine;

namespace GorillaHands;

public class HandStuckManager
{
    private HandController controller;
    private Rigidbody hand;

    public HandStuckManager(HandController handController, Rigidbody handRb)
    {
        controller = handController;
        hand = handRb;
    }

    public void CheckHandFreedom()  // https://youtu.be/cCHf8FxqzJc?t=106
    {
        var collider = controller.FollowerCollider;
        if (collider == null) return;

        Vector3 targetPosition = controller.TargetPosition;
        float distance = Vector3.Distance(hand.position, targetPosition);

        if (collider.gameObject.activeSelf)
        {
            if (distance > Configuration.HandStuckDistanceThreshold.Value)
            {
                SetCollidersActive(false);
            }
        }
        else if (distance <= 1.5f)
        {
            SetCollidersActive(true);
        }
    }

    private void SetCollidersActive(bool value)
    {
        controller.FollowerCollider.gameObject.SetActive(value);
    }
}
