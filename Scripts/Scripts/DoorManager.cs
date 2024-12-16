using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;

public class DoorManager : MonoBehaviour
{
    [System.Serializable]
    public class DoorGroup
    {
        public string groupName;
        public List<Door> doors = new List<Door>();
        public DoorGroupBehavior groupBehavior;
        public float activationDelay = 0f;
        public bool isActive = true;
    }

    [System.Serializable]
    public class DoorSequence
    {
        public string sequenceName;
        public List<DoorGroup> doorGroups = new List<DoorGroup>();
        public float timeBetweenGroups = 2f;
        public bool loopSequence = false;
    }

    public enum DoorGroupBehavior
    {
        AllOpen,
        AllClose,
        RandomOne,
        AlternateOpenClose,
        Pattern
    }

    [Header("Door Groups")]
    [SerializeField] private List<DoorGroup> doorGroups = new List<DoorGroup>();

    [Header("Door Sequences")]
    [SerializeField] private List<DoorSequence> doorSequences = new List<DoorSequence>();

    [Header("Settings")]
    [SerializeField] private float globalDoorDelay = 0.5f;
    [SerializeField] private bool randomizeOnStart = false;
    [SerializeField] private int maxSimultaneousOpenDoors = 3;

    [Header("Events")]
    public UnityEvent onAllDoorsOpened;
    public UnityEvent onAllDoorsClosed;

    private Dictionary<string, DoorGroup> doorGroupDictionary = new Dictionary<string, DoorGroup>();
    private Dictionary<string, DoorSequence> doorSequenceDictionary = new Dictionary<string, DoorSequence>();
    private List<Door> activeDoors = new List<Door>();
    private Coroutine currentSequence;

    private void Awake()
    {
        InitializeDictionaries();
    }

    private void Start()
    {
        if (randomizeOnStart)
        {
            RandomizeAllDoors();
        }
    }

    private void InitializeDictionaries()
    {
        doorGroupDictionary.Clear();
        doorSequenceDictionary.Clear();

        foreach (DoorGroup group in doorGroups)
        {
            if (!string.IsNullOrEmpty(group.groupName))
            {
                doorGroupDictionary[group.groupName] = group;
            }
        }

        foreach (DoorSequence sequence in doorSequences)
        {
            if (!string.IsNullOrEmpty(sequence.sequenceName))
            {
                doorSequenceDictionary[sequence.sequenceName] = sequence;
            }
        }
    }

    public void ActivateDoorGroup(string groupName)
    {
        if (doorGroupDictionary.TryGetValue(groupName, out DoorGroup group))
        {
            StartCoroutine(HandleDoorGroup(group));
        }
        else
        {
            Debug.LogWarning($"Door group '{groupName}' not found!");
        }
    }

    public void StartDoorSequence(string sequenceName)
    {
        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
        }

        if (doorSequenceDictionary.TryGetValue(sequenceName, out DoorSequence sequence))
        {
            currentSequence = StartCoroutine(RunDoorSequence(sequence));
        }
        else
        {
            Debug.LogWarning($"Door sequence '{sequenceName}' not found!");
        }
    }

    private IEnumerator HandleDoorGroup(DoorGroup group)
    {
        if (!group.isActive) yield break;

        yield return new WaitForSeconds(group.activationDelay);

        switch (group.groupBehavior)
        {
            case DoorGroupBehavior.AllOpen:
                foreach (Door door in group.doors)
                {
                    OpenDoor(door);
                    yield return new WaitForSeconds(globalDoorDelay);
                }
                break;

            case DoorGroupBehavior.AllClose:
                foreach (Door door in group.doors)
                {
                    CloseDoor(door);
                    yield return new WaitForSeconds(globalDoorDelay);
                }
                break;

            case DoorGroupBehavior.RandomOne:
                if (group.doors.Count > 0)
                {
                    Door randomDoor = group.doors[Random.Range(0, group.doors.Count)];
                    OpenDoor(randomDoor);
                }
                break;

            case DoorGroupBehavior.AlternateOpenClose:
                for (int i = 0; i < group.doors.Count; i++)
                {
                    if (i % 2 == 0)
                        OpenDoor(group.doors[i]);
                    else
                        CloseDoor(group.doors[i]);
                    yield return new WaitForSeconds(globalDoorDelay);
                }
                break;

            case DoorGroupBehavior.Pattern:
                yield return StartCoroutine(ExecutePattern(group));
                break;
        }
    }

    private IEnumerator RunDoorSequence(DoorSequence sequence)
    {
        do
        {
            foreach (DoorGroup group in sequence.doorGroups)
            {
                yield return StartCoroutine(HandleDoorGroup(group));
                yield return new WaitForSeconds(sequence.timeBetweenGroups);
            }
        }
        while (sequence.loopSequence);
    }

    private IEnumerator ExecutePattern(DoorGroup group)
    {
        // Example pattern: Open doors in a wave pattern
        int midPoint = group.doors.Count / 2;

        // Open from center
        for (int i = midPoint; i >= 0; i--)
        {
            if (i < group.doors.Count)
            {
                OpenDoor(group.doors[i]);
                if (i != midPoint)
                {
                    OpenDoor(group.doors[group.doors.Count - 1 - i]);
                }
                yield return new WaitForSeconds(globalDoorDelay);
            }
        }
    }

    private void OpenDoor(Door door)
    {
        if (activeDoors.Count >= maxSimultaneousOpenDoors)
        {
            CloseDoor(activeDoors[0]); // Close the oldest open door
        }

        if (!activeDoors.Contains(door))
        {
            activeDoors.Add(door);
            door.SendMessage("OpenDoor", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void CloseDoor(Door door)
    {
        if (activeDoors.Contains(door))
        {
            activeDoors.Remove(door);
            door.SendMessage("CloseDoor", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void RandomizeAllDoors()
    {
        foreach (DoorGroup group in doorGroups)
        {
            RandomizeDoorGroup(group);
        }
    }

    private void RandomizeDoorGroup(DoorGroup group)
    {
        foreach (Door door in group.doors)
        {
            if (Random.value > 0.5f)
            {
                OpenDoor(door);
            }
            else
            {
                CloseDoor(door);
            }
        }
    }

    public void LockAllDoors()
    {
        foreach (DoorGroup group in doorGroups)
        {
            foreach (Door door in group.doors)
            {
                door.SendMessage("Lock", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public void UnlockAllDoors()
    {
        foreach (DoorGroup group in doorGroups)
        {
            foreach (Door door in group.doors)
            {
                door.SendMessage("Unlock", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public void CloseAllDoors()
    {
        foreach (Door door in activeDoors.ToArray())
        {
            CloseDoor(door);
        }
        onAllDoorsClosed?.Invoke();
    }

    // Helper methods for external scripts
    public bool IsDoorGroupActive(string groupName)
    {
        return doorGroupDictionary.TryGetValue(groupName, out DoorGroup group) && group.isActive;
    }

    public void SetDoorGroupActive(string groupName, bool active)
    {
        if (doorGroupDictionary.TryGetValue(groupName, out DoorGroup group))
        {
            group.isActive = active;
        }
    }

    public void StopAllSequences()
    {
        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
            currentSequence = null;
        }
    }
}