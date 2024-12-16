using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private AIState currentState = AIState.Idle;
    [SerializeField] private AIBehaviorType behaviorType = AIBehaviorType.Patrol;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float fieldOfView = 90f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float fleeHealthThreshold = 0.3f;
    [SerializeField] private float healThreshold = 0.5f;

    [Header("Group Behavior")]
    [SerializeField] private bool enableGroupBehavior = false;
    [SerializeField] private float groupRadius = 5f;
    [SerializeField] private int maxGroupSize = 3;

    private NavMeshAgent agent;
    private Transform player;
    private Vector3 lastKnownPlayerPosition;
    private int currentPatrolIndex;
    private float currentHealth;
    private float maxHealth = 100f;
    private bool canAttack = true;
    private List<AIController> nearbyAllies;
    private Animator animator;

    public enum AIState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Flee,
        Search,
        Heal
    }

    public enum AIBehaviorType
    {
        Patrol,
        Aggressive,
        Defensive,
        Support
    }

    private void Start()
    {
        InitializeAI();
    }

    private void InitializeAI()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        nearbyAllies = new List<AIController>();
        currentHealth = maxHealth;

        agent.speed = moveSpeed;
        agent.angularSpeed = rotationSpeed;

        if (behaviorType == AIBehaviorType.Patrol && patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points assigned to " + gameObject.name);
        }
    }

    private void Update()
    {
        if (currentHealth <= 0) return;

        UpdateAIState();
        UpdateBehavior();
        UpdateAnimations();
    }

    private void UpdateAIState()
    {
        // Check for state transitions
        switch (currentState)
        {
            case AIState.Idle:
                if (CanSeePlayer())
                {
                    currentState = AIState.Chase;
                }
                else if (behaviorType == AIBehaviorType.Patrol)
                {
                    currentState = AIState.Patrol;
                }
                break;

            case AIState.Patrol:
                if (CanSeePlayer())
                {
                    currentState = AIState.Chase;
                }
                break;

            case AIState.Chase:
                if (!CanSeePlayer())
                {
                    currentState = AIState.Search;
                }
                else if (IsInAttackRange())
                {
                    currentState = AIState.Attack;
                }
                break;

            case AIState.Attack:
                if (!IsInAttackRange())
                {
                    currentState = AIState.Chase;
                }
                else if (ShouldFlee())
                {
                    currentState = AIState.Flee;
                }
                break;

            case AIState.Flee:
                if (ShouldHeal())
                {
                    currentState = AIState.Heal;
                }
                break;

            case AIState.Search:
                if (CanSeePlayer())
                {
                    currentState = AIState.Chase;
                }
                break;

            case AIState.Heal:
                if (currentHealth >= maxHealth * healThreshold)
                {
                    currentState = AIState.Chase;
                }
                break;
        }
    }

    private void UpdateBehavior()
    {
        switch (currentState)
        {
            case AIState.Idle:
                HandleIdle();
                break;

            case AIState.Patrol:
                HandlePatrol();
                break;

            case AIState.Chase:
                HandleChase();
                break;

            case AIState.Attack:
                HandleAttack();
                break;

            case AIState.Flee:
                HandleFlee();
                break;

            case AIState.Search:
                HandleSearch();
                break;

            case AIState.Heal:
                HandleHeal();
                break;
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = player.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle <= fieldOfView * 0.5f)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRange))
            {
                if (hit.transform == player)
                {
                    lastKnownPlayerPosition = player.position;
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsInAttackRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    private bool ShouldFlee()
    {
        return currentHealth <= maxHealth * fleeHealthThreshold;
    }

    private bool ShouldHeal()
    {
        return currentHealth <= maxHealth * healThreshold;
    }

    private void HandleIdle()
    {
        agent.isStopped = true;
        // Add idle behavior here
    }

    private void HandlePatrol()
    {
        if (patrolPoints.Length == 0) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        yield return new WaitForSeconds(patrolWaitTime);

        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void HandleChase()
    {
        if (player != null)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    private void HandleAttack()
    {
        if (canAttack && player != null)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        canAttack = false;

        // Perform attack logic here
        // Example: Apply damage to player
        var playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void HandleFlee()
    {
        if (player != null)
        {
            Vector3 fleeDirection = transform.position - player.position;
            Vector3 fleePosition = transform.position + fleeDirection.normalized * detectionRange;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePosition, out hit, detectionRange, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void HandleSearch()
    {
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // Generate random search position around last known player position
            Vector3 randomSearchPoint = lastKnownPlayerPosition + Random.insideUnitSphere * detectionRange;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomSearchPoint, out hit, detectionRange, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void HandleHeal()
    {
        agent.isStopped = true;
        currentHealth += Time.deltaTime * 10f; // Heal rate
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
            animator.SetBool("IsAttacking", currentState == AIState.Attack);
            animator.SetBool("IsHealing", currentState == AIState.Heal);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (currentState == AIState.Idle || currentState == AIState.Patrol)
        {
            currentState = AIState.Chase;
        }
    }

    private void Die()
    {
        // Handle death
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        agent.isStopped = true;
        enabled = false;

        // Optional: Spawn effects, drop items, etc.
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(3f); // Adjust delay as needed
        Destroy(gameObject);
    }

    private void UpdateGroupBehavior()
    {
        if (!enableGroupBehavior) return;
    }