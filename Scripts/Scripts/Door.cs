using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private DoorType doorType = DoorType.Normal;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 2f;

    [Header("Fake Door Settings")]
    [SerializeField] private bool isFake = false;
    [SerializeField] private float breakForce = 5f;
    [SerializeField] private GameObject doorVisuals;
    [SerializeField] private GameObject brokenDoorPrefab;

    [Header("Timed Door Settings")]
    [SerializeField] private float autoCloseDelay = 3f;
    [SerializeField] private bool autoClose = false;

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip breakSound;

    private AudioSource audioSource;
    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector3 initialRotation;
    private Vector3 targetRotation;
    private float currentRotation = 0f;

    public enum DoorType
    {
        Normal,
        Fake,
        Timed
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        initialRotation = transform.eulerAngles;
        targetRotation = initialRotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HandleDoorInteraction(other);
        }
    }

    private void HandleDoorInteraction(Collider player)
    {
        if (isLocked || isAnimating) return;

        switch (doorType)
        {
            case DoorType.Normal:
                ToggleDoor();
                break;

            case DoorType.Fake:
                if (isFake)
                {
                    BreakDoor();
                }
                else
                {
                    ToggleDoor();
                }
                break;

            case DoorType.Timed:
                OpenDoor();
                if (autoClose)
                {
                    StartCoroutine(AutoCloseDoor());
                }
                break;
        }
    }

    private void ToggleDoor()
    {
        if (!isOpen)
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        if (isOpen || isAnimating) return;

        isAnimating = true;
        targetRotation = initialRotation + new Vector3(0, openAngle, 0);
        StartCoroutine(AnimateDoor(true));

        if (openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
    }

    private void CloseDoor()
    {
        if (!isOpen || isAnimating) return;

        isAnimating = true;
        targetRotation = initialRotation;
        StartCoroutine(AnimateDoor(false));

        if (closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
    }

    private IEnumerator AnimateDoor(bool opening)
    {
        float startRotation = transform.eulerAngles.y;
        float endRotation = targetRotation.y;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * openSpeed;
            float currentYRotation = Mathf.Lerp(startRotation, endRotation, elapsedTime);
            transform.eulerAngles = new Vector3(
                transform.eulerAngles.x,
                currentYRotation,
                transform.eulerAngles.z
            );
            yield return null;
        }

        transform.eulerAngles = targetRotation;
        isAnimating = false;
        isOpen = opening;
    }

    private void BreakDoor()
    {
        if (breakSound != null)
        {
            audioSource.PlayOneShot(breakSound);
        }

        // Disable the door visuals
        if (doorVisuals != null)
        {
            doorVisuals.SetActive(false);
        }

        // Spawn broken door pieces
        if (brokenDoorPrefab != null)
        {
            GameObject brokenDoor = Instantiate(brokenDoorPrefab, transform.position, transform.rotation);

            // Add forces to the broken pieces
            Rigidbody[] pieces = brokenDoor.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody piece in pieces)
            {
                Vector3 randomDirection = Random.insideUnitSphere;
                piece.AddForce(randomDirection * breakForce, ForceMode.Impulse);
                piece.AddTorque(randomDirection * breakForce, ForceMode.Impulse);
            }

            // Destroy broken pieces after delay
            Destroy(brokenDoor, 3f);
        }

        // Disable this door's collider
        Collider doorCollider = GetComponent<Collider>();
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }
    }

    private IEnumerator AutoCloseDoor()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        CloseDoor();
    }

    public void Lock()
    {
        isLocked = true;
    }

    public void Unlock()
    {
        isLocked = false;
    }

    // Visual feedback for fake doors (called in Update if needed)
    private void UpdateVisualFeedback()
    {
        if (doorType == DoorType.Fake && isFake)
        {
            // Add visual hints that the door might be fake
            // For example, slight wobble or color change
            float wobble = Mathf.Sin(Time.time * 2f) * 0.2f;
            doorVisuals.transform.localRotation = Quaternion.Euler(0, wobble, 0);
        }
    }
}

