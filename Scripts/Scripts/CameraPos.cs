using UnityEngine;
using System.Collections;

public class CameraPos : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -9f);
    [SerializeField] private bool followBehind = true;

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 5f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector2 rotationLimits = new Vector2(45f, 85f);

    [Header("Camera Controls")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float scrollSensitivity = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private bool invertY = false;

    [Header("Camera Shake")]
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float shakeDuration = 0.5f;

    [Header("Collision")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private float collisionOffset = 0.2f;
    [SerializeField] private LayerMask collisionLayers;

    private float currentZoom;
    private float currentX;
    private float currentY;
    private Vector3 currentVelocity;
    private bool isShaking;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("No target assigned to CameraPos!");
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        currentZoom = offset.magnitude;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Initialize camera position
        if (target != null)
        {
            transform.position = target.position + offset;
            if (lookAtTarget)
            {
                transform.LookAt(target);
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        FollowTarget();

        if (enableCollision)
        {
            HandleCollision();
        }
    }

    private void HandleInput()
    {
        // Mouse rotation
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
            currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity * (invertY ? -1 : 1);
            currentY = Mathf.Clamp(currentY, rotationLimits.x, rotationLimits.y);
        }

        // Zoom with scroll wheel
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;
        currentZoom = Mathf.Clamp(currentZoom - scrollDelta, minZoom, maxZoom);
    }

    private void FollowTarget()
    {
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Calculate position
        Vector3 targetPosition = target.position;
        Vector3 desiredPosition;

        if (followBehind)
        {
            // Position camera behind target based on target's rotation
            Vector3 directionFromTarget = rotation * Vector3.forward;
            desiredPosition = targetPosition - directionFromTarget * currentZoom + Vector3.up * offset.y;
        }
        else
        {
            // Position camera based on offset and rotation
            desiredPosition = targetPosition + rotation * new Vector3(0, offset.y, -currentZoom);
        }

        // Smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);

        // Handle rotation
        if (lookAtTarget)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotationSmoothSpeed);
        }
    }

    private void HandleCollision()
    {
        Vector3 directionToTarget = target.position - transform.position;
        float targetDistance = directionToTarget.magnitude;

        // Check for collision
        RaycastHit hit;
        if (Physics.Raycast(target.position, -directionToTarget.normalized, out hit,
            targetDistance, collisionLayers))
        {
            transform.position = hit.point + hit.normal * collisionOffset;
        }
    }

    public void ShakeCamera()
    {
        if (!isShaking)
        {
            StartCoroutine(CameraShakeCoroutine());
        }
    }

    private IEnumerator CameraShakeCoroutine()
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float strength = (shakeDuration - elapsed) / shakeDuration;

            transform.position = originalPosition + Random.insideUnitSphere * shakeIntensity * strength;
            transform.rotation = originalRotation * Quaternion.Euler(
                Random.Range(-1f, 1f) * shakeIntensity * strength * 15f,
                Random.Range(-1f, 1f) * shakeIntensity * strength * 15f,
                Random.Range(-1f, 1f) * shakeIntensity * strength * 15f
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        isShaking = false;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    public void ResetCamera()
    {
        currentX = 0f;
        currentY = 0f;
        currentZoom = offset.magnitude;
        transform.position = target.position + offset;
        if (lookAtTarget)
        {
            transform.LookAt(target);
        }
    }

    // Camera effects
    public void ZoomTo(float targetZoom, float duration)
    {
        StartCoroutine(ZoomCoroutine(targetZoom, duration));
    }

    private IEnumerator ZoomCoroutine(float targetZoom, float duration)
    {
        float startZoom = currentZoom;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            currentZoom = Mathf.Lerp(startZoom, targetZoom, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentZoom = targetZoom;
    }
}