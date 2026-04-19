using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyBossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform effectsSpawnPoint;
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 300f;

    [Header("Melee")]
    [SerializeField] private float meleeRange = 2.5f;
    [SerializeField] private float meleeDamage = 20f;
    [SerializeField] private float meleeCooldown = 1.5f;

    [Header("Targeting")]
    [SerializeField] private float targetRefreshInterval = 0.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Phases")]
    [SerializeField] private BossPhaseBase[] phases;

    private NavMeshAgent agent;
    private Animator animator;

    private float currentHealth;
    private Transform currentTarget;
    private BossPhaseBase currentPhase = null;

    private float meleeTimer;
    private float specialTimer;
    private float targetUpdateTimer;

    private bool isDead = false;
    private bool isBusy = false;

    //public float GetHealthPercent => (maxHealth <= 0f) ? 0f : (currentHealth / maxHealth);
    public Transform EffectsSpawnPoint => effectsSpawnPoint;
    public Animator Animator => animator;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        currentHealth = maxHealth;
        ResetAllPhases();
        UpdatePhase();
    }

    private void Update()
    {
        if (isDead) return;

        UpdateTimers();
        UpdateTarget();
        UpdatePhase();
        UpdateAnimator();
        HandleCombat();
    }

    /**
     * Desactiva todas las fases del Boss. TODO: Eliminar metodo
     */
    private void ResetAllPhases()
    {
        if (phases == null) return;

        foreach (BossPhaseBase phase in phases)
        {
            if (phase != null)
                phase.enabled = false;
        }
    }

    private void UpdateTimers()
    {
        meleeTimer -= Time.deltaTime;
        specialTimer -= Time.deltaTime;
        targetUpdateTimer -= Time.deltaTime;
    }

    /**
     * Elige/Actualiza su objetivo dependiendo de la distancia del jugador
     */
    private void UpdateTarget()
    {
        if (targetUpdateTimer > 0f && currentTarget != null) return;

        targetUpdateTimer = targetRefreshInterval;
        currentTarget = GetClosestAlivePlayer();
    }

    /**
     * Elige/Actualiza la fase del Boss dependiendo de su vida actual
     */
    private void UpdatePhase()
    {
        BossPhaseBase newPhase = GetPhaseFromHealth();

        if (currentPhase == newPhase) return;

        if (currentPhase != null)
        {
            currentPhase.OnExitPhase(this);
            currentPhase.enabled = false;
        }

        currentPhase = newPhase;

        if (currentPhase != null)
        {
            currentPhase.enabled = true;
            currentPhase.OnEnterPhase(this);

            agent.speed = currentPhase.MoveSpeed;
        }

        specialTimer = currentPhase.SpecialCooldown * 0.5f;
    }

    /**
     * Busca la Phase a realizar en funcion de la vida actual
     */
    private BossPhaseBase GetPhaseFromHealth()
    {
        if (phases == null || phases.Length == 0) return null;

        //float healthPercent = GetHealthPercent;

        foreach (BossPhaseBase phase in phases)
        {
            if (phase != null && phase.IsInPhase(currentHealth))
                return phase;
        }

        return phases[phases.Length - 1];
    }

    /**
     * Actualiza el Animator: Idle - Walk - Run
     */
    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    /**
     * Elige/Busca el Player (!death) mas cercano 
     */
    public Transform GetClosestAlivePlayer()
    {
        var players = PlayerRegistry.Players;
        if (players == null || players.Count == 0) return null;

        Transform closest = null;
        float closestSqrDistance = float.MaxValue;

        foreach (Transform player in players)
        {
            if (!player) continue;
            //TODO: Revisar que el jugadir esta vivo

            float sqrDistance = (player.position - transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = player;
            }
        }

        return closest;
    }

    //TODO: Hacer un nuevo metodo: GetRandomAlivePlayer() o GetLessHealthPlayer()

    /**
     * Decide el ataque a realizar (Ninguno, Melee, Especial)
     */
    private void HandleCombat()
    {
        if (currentTarget == null || isBusy)
        {
            StopMovement();
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (CanUseSpecial(distance))
        {
            StartCoroutine(PerformSpecialAttack());
            return;
        }

        if (CanUseMelee(distance))
        {
            StartCoroutine(PerformMeleeAttack());
            return;
        }

        ChaseTarget();
    }

    private void ChaseTarget()
    {
        if (currentTarget == null) return;

        agent.isStopped = false;
        agent.SetDestination(currentTarget.position);
        RotateTowards(currentTarget.position);
    }

    public void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void StopMovement()
    {
        agent.ResetPath();
        agent.isStopped = true;
    }

    private bool CanUseMelee(float distance)
    {
        return (meleeTimer <= 0f) && (distance <= meleeRange);
    }

    /**
     * Utiliza las reglas propias de la Phase actual para decidir si se puede usar
     */
    private bool CanUseSpecial(float distance)
    {
        if (currentPhase == null) return false;
        if (specialTimer > 0f) return false;

        return currentPhase.CanUseSpecial(this, currentTarget, distance);
    }

    private IEnumerator PerformMeleeAttack()
    {
        isBusy = true;
        meleeTimer = meleeCooldown;

        StopMovement();

        if (currentTarget != null)
            RotateTowards(currentTarget.position);

        animator.SetTrigger("Attack");

        //TODO: Lanzar efecto visual VFX
        //TODO: Que sea el efecto visual quien haga daño con un colider

        //O utilizar un MeleeHitBox

        yield return new WaitForSeconds(0.25f);

        //DealMeleeDamage();

        yield return new WaitForSeconds(0.25f);

        isBusy = false;
    }

    private IEnumerator PerformSpecialAttack()
    {
        isBusy = true;
        specialTimer = currentPhase.SpecialCooldown;

        StopMovement();

        if (currentTarget != null)
            RotateTowards(currentTarget.position);

        yield return currentPhase.ExecuteSpecial(this, currentTarget);

        yield return new WaitForSeconds(0.25f);

        isBusy = false;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        //animator.SetTrigger("Hit");

        currentHealth -= damage;

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        isDead = true;
        currentHealth = 0f;

        StopMovement();

        animator.SetBool("IsDead", true);
    }
}
