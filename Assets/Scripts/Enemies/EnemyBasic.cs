using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyBasic : MonoBehaviour
{
    [Header("General")]
    [SerializeField] protected EnemyState state = EnemyState.Idle;
    //public EnemyState State => state; // Por si queremos hacer el estado publico
    public float health = 100f;

    [Header("Detection")]
    public float detectionRange = 6f;
    public float loseTargetRange = 10f;

    [Header("Movement")]
    public bool usePatrol = false;
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;

    [Header("Attack")]
    public float attackCooldown = 1.2f;
    public float damage = 10f;
    public bool canMelee = true;
    public float meleeRange = 1.5f;
    public bool canRanged = false;
    public float rangedRange = 6f;

    [Header("Melee Timing Hitbox")]
    public float attackWindup = 0.5f;   // Tiempo antes del impacto
    public float attackActiveTime = 0.25f; // Tiempo que el hitbox está activo
    public GameObject meleeHitbox;

    [Header("Ranged Attack")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float projectileSpeed = 10f;

    [Header("Ranged Timing")]
    public float rangedWindup = 0.35f;     // tiempo antes de disparar (sincroniza con anim)

    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform playerTarget;

    [Header("Targeting")]
    [SerializeField] private float retargetInterval = 0.5f;
    [SerializeField, Range(0.5f, 0.95f)]
    private float retargetSwitchBias = 0.75f; // 0.75 = el nuevo debe estar al menos 25% más cerca
    private float retargetTimer;

    protected int currentPatrolIndex = 0;
    protected float lastAttackTime;
    protected float patrolTimer;
    protected bool isAttacking = false;
    protected Coroutine meleeRoutine; //Referencia para poder cancelarla
    protected Coroutine rangedRoutine; //Referencia para poder cancelarla
    protected bool attackInterrupted;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        SetState(usePatrol && patrolPoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle);

        if (meleeHitbox != null)
        {
            meleeHitbox.GetComponent<MeleeHitbox>()?.SetDamage(damage);
        }
    }

    public void SpawnInit(bool patrol, Transform[] points) 
    {
        if (!patrol || points == null || points.Length == 0)
        {
            usePatrol = false;
            patrolPoints = null;
        } else
        {
            usePatrol = patrol;
            patrolPoints = points;
        }
    }

    protected virtual void Update()
    {
        if (state == EnemyState.Dead) return;

        if (!UpdateTargeting())
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        switch (state)
        {
            case EnemyState.Idle:
                UpdateIdle(distanceToPlayer);
                break;

            case EnemyState.Patrol:
                UpdatePatrol(distanceToPlayer);
                break;

            case EnemyState.Chase:
                UpdateChase(distanceToPlayer);
                break;

            case EnemyState.Attack:
                UpdateAttack(distanceToPlayer);
                break;
        }

        float normalizedSpeed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("speed", normalizedSpeed);
    }

    // ---------- ALL STATES ----------
    protected void SetState(EnemyState newState)
    {
        if (state == newState) return;

        EnemyState previous = state;
        state = newState;
        OnStateChanged(previous, newState);
    }

    protected virtual void OnStateChanged(EnemyState previous, EnemyState next)
    {
        // Base no hace nada por defecto
    }

    protected virtual void UpdateIdle(float distance)
    {
        if (distance <= detectionRange)
        {
            SetState(EnemyState.Chase);
        }
    }

    protected virtual void UpdatePatrol(float distance)
    {
        if (distance <= detectionRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (patrolPoints.Length == 0) return;

        agent.isStopped = false;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolTimer += Time.deltaTime;

            if (patrolTimer >= patrolWaitTime)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                patrolTimer = 0f;
            }
        }
    }

    protected virtual void UpdateChase(float distance)
    {
        if (distance > loseTargetRange)
        {
            SetState(usePatrol ? EnemyState.Patrol : EnemyState.Idle);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(playerTarget.position);

        if (CanAttack(distance))
        {
            SetState(EnemyState.Attack);
        }
    }

    protected virtual void UpdateAttack(float distance)
    {
        agent.isStopped = true;
        transform.LookAt(new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z));

        if (!CanAttack(distance))
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack(distance);
            lastAttackTime = Time.time;
        }
    }

    // ---------- LOGIC ----------
    protected bool UpdateTargeting()
    {
        // Re-selección de objetivo (barata y estable)
        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            bool force = (state == EnemyState.Idle || state == EnemyState.Patrol);
            AcquireTarget(false);
            retargetTimer = retargetInterval;
        }

        // No hay jugadores válidos
        if (!playerTarget)
        {
            SetState(usePatrol ? EnemyState.Patrol : EnemyState.Idle);
            agent.isStopped = true;
            animator.SetFloat("speed", 0f);
            return false;
        }

        return true;
    }

    protected bool CanAttack(float distance)
    {
        if (canMelee && distance <= meleeRange)
            return true;

        if (canRanged && distance <= rangedRange)
            return true;

        return false;
    }

    protected virtual void PerformAttack(float distance)
    {
        if (canMelee && distance <= meleeRange)
        {
            MeleeAttack();
        }
        else if (canRanged && distance <= rangedRange)
        {
            RangedAttack();
        }
    }

    protected virtual void MeleeAttack()
    {
        if (isAttacking) return;

        attackInterrupted = false;
        meleeRoutine = StartCoroutine(MeleeAttackRoutine());
    }

    protected virtual void RangedAttack()
    {
        if (isAttacking) return;
        if (projectilePrefab == null || shootPoint == null || playerTarget == null) return;

        attackInterrupted = false;
        rangedRoutine = StartCoroutine(RangedAttackRoutine());
    }

    // ---------- DAMAGE / DEATH ----------

    public virtual void TakeDamage(float dmg)
    {
        if (state == EnemyState.Dead) return;

        health -= dmg;

        if (isAttacking)
            InterruptAttack();

        animator.SetTrigger("Hit");

        if (state == EnemyState.Idle || state == EnemyState.Patrol)
            SetState(EnemyState.Chase);

        if (health <= 0)
            Die();
    }

    protected virtual void InterruptAttack()
    {
        attackInterrupted = true;

        if (meleeRoutine != null)
        {
            StopCoroutine(meleeRoutine);
            meleeRoutine = null;
        }

        if (rangedRoutine != null)
        {
            StopCoroutine(rangedRoutine);
            rangedRoutine = null;
        }

        if (canMelee)
            meleeHitbox?.SetActive(false);

        isAttacking = false;

        //Ajustamos el tiempo para que se recupere un poco antes
        lastAttackTime = Time.time - (attackCooldown * 0.5f);
    }

    protected virtual void Die()
    {
        SetState(EnemyState.Dead);
        agent.isStopped = true;
        animator?.SetTrigger("Die");

        //TODO: Hacer que cada X segundos se hunda en el suelo o FadeOut()

        Destroy(gameObject, 3f);
    }

    protected IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;
        animator?.SetTrigger("BasicAttack");

        // Tiempo de preparación (windup)
        yield return new WaitForSeconds(attackWindup);

        meleeHitbox?.SetActive(true);

        // Tiempo activo del golpe
        yield return new WaitForSeconds(attackActiveTime);

        // Desactivar hitbox
        meleeHitbox?.SetActive(false);

        // Cooldown restante
        yield return new WaitForSeconds(Mathf.Max(0, attackCooldown - attackWindup - attackActiveTime));

        isAttacking = false;
        meleeRoutine = null;
    }

    protected virtual IEnumerator RangedAttackRoutine()
    {
        isAttacking = true;
        animator?.SetTrigger("BasicAttack");

        // Windup (sincronizar con anim)
        yield return new WaitForSeconds(rangedWindup);

        if (attackInterrupted || playerTarget == null)
        {
            isAttacking = false;
            yield break;
        }

        // Dirección y rotación "tipo Player"
        Vector3 dir = playerTarget.position - shootPoint.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        dir.Normalize();

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        GameObject go = Instantiate(projectilePrefab, shootPoint.position, rot);

        // Reutiliza tu script de proyectil (ideal: renómbralo a Projectile, pero sirve igual)
        var proj = go.GetComponent<EnemyProjectile>();
        if (proj != null)
        {
            proj.Init(dir, projectileSpeed, damage);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = dir * projectileSpeed;
        }

        isAttacking = false;
        rangedRoutine = null;
    }

    protected void AcquireTarget(bool force)
    {
        var best = ChooseTarget();

        if (!best) { playerTarget = null; return; }

        if (force || !playerTarget) { playerTarget = best; return; }

        // Se queda solo con el nuevo si hay una diferencia notable
        float bestSqr = (best.position - transform.position).sqrMagnitude;
        float currentSqr = (playerTarget.position - transform.position).sqrMagnitude;

        if (best != playerTarget && bestSqr < currentSqr * retargetSwitchBias)
            playerTarget = best;
    }

    protected virtual Transform ChooseTarget()
    {
        var players = PlayerRegistry.Players;
        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (!p) continue;

            float sqr = (p.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = p;
            }
        }

        return best;
    }

    // ---------- DEBUG ----------

    void OnDrawGizmosSelected()
    {
        // Rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Rango de persecución
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        // Rango de ataque
        Gizmos.color = Color.red;
        if (canMelee)
        {
            Gizmos.DrawWireSphere(transform.position, meleeRange);
        }

        if (canRanged)
        {
            Gizmos.DrawWireSphere(transform.position, rangedRange);
        }
    }
}
