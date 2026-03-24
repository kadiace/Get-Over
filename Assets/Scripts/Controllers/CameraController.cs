using UnityEngine;

public sealed class CameraController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform followTarget;

    [Header("Colony Constraint")]
    [SerializeField] private float playerToAxisRatioA = 1f;
    [SerializeField] private float playerToAxisRatioB = 3f;
    [SerializeField] private float yOffsetFromPlayer = -3f;
    private Vector3 _lastRadialDirection = Vector3.right;

    public float Yaw => Mathf.Atan2(transform.forward.z, transform.forward.x) * Mathf.Rad2Deg;
    public Transform FollowTarget => followTarget;

    private void Start()
    {
        if (followTarget == null)
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
                followTarget = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
                return;

            followTarget = player.transform;
        }

        Vector3 targetPosition = followTarget.position;
        Vector3 playerRadial = new(targetPosition.x, 0f, targetPosition.z);

        if (playerRadial.sqrMagnitude > Define.epsilon)
            _lastRadialDirection = playerRadial.normalized;

        float ratioSum = Mathf.Max(Define.epsilon, playerToAxisRatioA + playerToAxisRatioB);
        Vector3 chosenRadial = playerRadial * (playerToAxisRatioB / ratioSum);

        if (chosenRadial.sqrMagnitude <= Define.epsilon)
            chosenRadial = _lastRadialDirection;

        Vector3 cameraPosition = new(
            chosenRadial.x,
            targetPosition.y + yOffsetFromPlayer,
            chosenRadial.z);

        transform.position = cameraPosition;

        Vector3 toTarget = targetPosition - cameraPosition;
        if (toTarget.sqrMagnitude <= Define.epsilon)
            return;

        transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
    }
}
