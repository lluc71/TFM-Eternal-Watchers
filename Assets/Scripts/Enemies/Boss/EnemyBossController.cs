using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyBossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponSpawnPoint;
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 300f;

    [Header("Melee")]
    [SerializeField] private float meleeRange = 2.5f;
    [SerializeField] private float meleeDamage = 20f;
    [SerializeField] private float meleeCooldown = 1.5f;

    [Header("Melee Hitbox")]
    [SerializeField] private BossMeleeHitbox meleeHitbox;
    [SerializeField] private float meleeHitboxActiveTime = 0.25f;

    [Header("Targeting")]
    [SerializeField] private float targetRefreshInterval = 0.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Phases")]
    [SerializeField] private BossPhaseBase[] phases;

    private NavMeshAgent agent;
    private Animator animator;
    private GUIBossInfo bossInfoGUI;

    private float currentHealth;
    private Transform currentTarget;
    private BossPhaseBase currentPhase = null;

    private float meleeTimer;
    private float specialTimer;
    private float targetUpdateTimer;

    private bool isDead = false;
    private bool isBusy = false;

    //TODO: Mirar si basta usar el isBusy o no
    private bool isPerformingMelee = false;
    private bool isPerformingSpecial = false;

    //public float GetHealthPercent => (maxHealth <= 0f) ? 0f : (currentHealth / maxHealth);
    public Transform WeaponSpawnPoint => weaponSpawnPoint;
    public Transform ProjectileSpawnPoint => projectileSpawnPoint;
    public Animator Animator => animator;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        currentHealth = maxHealth;
        ResetAllPhases();
        UpdatePhase();

        meleeHitbox?.Initialize(meleeDamage);

        GUIManager.Instance.RegisterBoss(this);
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
            StartSpecialAttack();
            return;
        }

        if (CanUseMelee(distance))
        {
            StartMeleeAttack();
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

    /**
     * Rotacion extrema. Solo usar antes de un Special.
     */
    public void ForceRotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = targetRotation;
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

    /**
     * Lanzamos la animacion y en ella usamos Events para activar el hitbox
     */
    private void StartMeleeAttack()
    {
        isBusy = true;
        isPerformingMelee = true;
        meleeTimer = meleeCooldown;

        StopMovement();

        if (currentTarget != null)
            RotateTowards(currentTarget.position);

        animator.SetTrigger("Attack");
    }

    /**
     * Lanzamos la animacion del Special Attack de la fase actual
     */
    private void StartSpecialAttack()
    {
        isBusy = true;
        isPerformingSpecial = true;
        specialTimer = currentPhase.SpecialCooldown;

        StopMovement();

        if (currentTarget != null)
            ForceRotateTowards(currentTarget.position);

        animator.SetTrigger(currentPhase.GetAnimatorTriggerName());
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        //animator.SetTrigger("Hit");

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        bossInfoGUI?.UpdateHealth(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        isDead = true;
        currentHealth = 0f;
        GUIManager.Instance.HideBossPanel();

        StopMovement();

        //Desactiva los colliders del boss
        meleeHitbox?.Disable();
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        animator.SetTrigger("Die");
    }

    public void SetBossInfoGUI(GUIBossInfo panel)
    {
        bossInfoGUI = panel;

        int phaseCount = (phases != null) ? phases.Length : 0;
        bossInfoGUI?.Setup(maxHealth, phaseCount);
    }


    //----------- ANIMATION EVENTS --------------
    //- Todos cuentan con un Start i con un End -

    /**
     * Evento Inicio para el ataque normal.
     * Activamos el Hitbox el tiempo definido.
     */
    public void Event_MeleeAttackStart()
    {
        if (!isPerformingMelee || isDead) return;

        meleeHitbox?.ActivateHitbox(meleeHitboxActiveTime);
    }

    /**
     * Evento Fin para el ataque normal.
     * Limpiamos flags para salir del estado actual.
     */
    public void Event_MeleeAttackFinished()
    {
        if (!isPerformingMelee) return;

        isPerformingMelee = false;
        isBusy = false;
    }

    /**
     * Evento Inicio para el ataque especial.
     * Llamamos al ExecuteSpecialImpact de la fase actual.
     */
    public void Event_SpecialStart()
    {
        if (!isPerformingSpecial || isDead) return;
        if (currentPhase == null) return;

        currentPhase.ExecuteSpecialImpact(this, currentTarget);
    }

    /**
     * Evento Fin para el ataque especial.
     * Limpiamos flags para salir del estado actual.
     */
    public void Event_SpecialFinished()
    {
        if (!isPerformingSpecial) return;

        isPerformingSpecial = false;
        isBusy = false;
    }
}
