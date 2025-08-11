using UnityEngine;

namespace GorillaHands;

public class HandStuckManager
{
    private HandController controller;
    private Rigidbody hand;

    private bool isStuck;
    private bool reachedTarget;

    public HandStuckManager(HandController handController, Rigidbody handRb)
    {
        controller = handController;
        hand = handRb;
    }

    public void CheckHandFreedom()  // https://youtu.be/cCHf8FxqzJc?t=106
    {
        Vector3 targetPosition = controller.TargetPosition;
        float distance = Vector3.Distance(hand.position, targetPosition);

        // This is just testing code, dont panic
        if (isStuck)
        {
            if (reachedTarget)
            {
                SetCollidersActive(true);
                isStuck = false;
                reachedTarget = false;
                return;
            }
            else if (distance <= 1)
            {
                reachedTarget = true;
            }
            return;
        }

        if (distance > Configuration.HandStuckDistanceThreshold.Value)
        {
            isStuck = true;
            SetCollidersActive(false);
        }
    }

    private void SetCollidersActive(bool value)
    {
        if (controller.FollowerCollider is null) return;
        controller.FollowerCollider.gameObject.SetActive(value);
    }
}
