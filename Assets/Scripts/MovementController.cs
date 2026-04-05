using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class MovementController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float blockSpeed = 3f;
    public float rotationSpeed = 10f;
    private float verticalVelocity = 0f;

    [Header("Ground Check")]
    public LayerMask groundMask;
    public Transform groundCheck;
    public float groundDistance = 0.2f;

    [Header("Shield")]
    [SerializeField] private List<GameObject> shieldAura = new List<GameObject>();
    public int maxShieldHits = 3;
    public float shieldRegenTime = 5f;

    private int currentShieldHits = 0;
    private bool shieldActive = true;
    private float lastShieldHitTime = -10f;
    private Coroutine shieldRegenRoutine;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    private GUIPlayerInfo playerInfoGUI;
    public bool isDead { get; private set; }

    private CharacterController controller;
    private Camera cam;
    private Animator animator;
    private PlayerInput playerInput;
    private InputAction blockAction;
    private PauseManager pauseManager;

    Vector2 moveInput;
    private Vector2 stickInputPos;
    private Vector2 mouseScreenPos;
    private bool isBlocking = false;
    private bool isGrounded;

    [Header("Rotation Gamepad")]
    public float gamepadStickDeadzone = 0.25f;
    private bool usingGamepad;

    [Header("Attack Mode")]
    [SerializeField] private bool useMeleeAttack = true;
    public float damage = 10f;

    [Header("Melee - Combo System")]
    public int maxCombo = 3;
    public float comboResetTime = 0.8f;   // tiempo para encadenar el siguiente golpe
    public float fatigueTime = 1f;      // tiempo cansado tras 3 golpes

    [Header("Melee - Hitbox")]
    public GameObject meleeHitbox;
    public float attackWindup = 0.2f;
    public float attackActiveTime = 0.2f;
    public float attackCooldown = 0.1f;

    private bool isAttackPerforming = false;
    private int comboStep = 0;
    private float lastAttackTime = -10f;
    private bool isFatigued = false;

    [Header("Melee - VFX")]
    [SerializeField] private List<GameObject> meleeVfxPrefabs = new List<GameObject>();
    [SerializeField] private Transform meleeVfxSpawnPoint;

    [Header("Ranged - Projectile")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;


    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float shotCooldown = 0.4f;
    [Tooltip("Opcional: retraso antes de disparar (para animación).")]
    [SerializeField] private float shotWindup = 0.1f;
    [SerializeField] private string shootTriggerName = "Shoot";

    private float lastShotTime = -10f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main;
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        blockAction = playerInput?.actions["Block"];

        currentHealth = maxHealth;

        if(useMeleeAttack)
        {
            if (meleeHitbox == null) return; //Mostrar error

            meleeHitbox.GetComponent<PlayerMeleeHitbox>()?.SetDamage(damage);
            meleeHitbox.SetActive(false);
        }

        pauseManager = FindFirstObjectByType<PauseManager>();
    }

    private void Start()
    {
        GUIManager.Instance.RegisterPlayer(this, playerInput);
    }

    void Update()
    {
        if (isDead) return;

        if (pauseManager != null && pauseManager.isPaused)
            return;

        //Sustituye el OnBlock()
        if (blockAction != null && !isAttackPerforming)
        {
            isBlocking = blockAction?.ReadValue<float>() > 0.5f;
            animator.SetBool("IsBlocking", isBlocking);
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        Move();

        if (usingGamepad)
        {
            LookWithStick();
        } else
        {
            LookAtMouse();
        }

        UpdateShieldVisual();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        stickInputPos = value.Get<Vector2>();

        if (stickInputPos.sqrMagnitude > gamepadStickDeadzone * gamepadStickDeadzone)
            usingGamepad = true;
    }

    public void OnLookMouse(InputValue value)
    {
        mouseScreenPos = value.Get<Vector2>();
        usingGamepad = false;
    }

    public void OnAttack(InputValue value)
    {
        if (isDead) return;
        if (!value.isPressed) return;
        if (isBlocking) return;
        if (isAttackPerforming) return;

        if (useMeleeAttack)
        {
            DoMeleeAttack();
        } else
        {
            DoRangedAttack();
        }
    }

    private void DoMeleeAttack()
    {
        if (isFatigued) return;

        float timeSinceLast = Time.time - lastAttackTime;

        // Si tardo mucho entre golpes, reinicia combo
        if (timeSinceLast > comboResetTime)
            comboStep = 0;

        comboStep++;

        if (comboStep > maxCombo)
            comboStep = 1;

        animator.SetInteger("ComboStep", comboStep);
        animator.SetTrigger("BasicAttack");

        lastAttackTime = Time.time;

        StartCoroutine(MeleeAttackRoutine(comboStep));

        // Si alcanzo el ultimo golpe: entra en fatiga
        if (comboStep == maxCombo)
        {
            isFatigued = true;
            Invoke(nameof(ResetFatigue), fatigueTime);
        }
    }

    private void DoRangedAttack()
    {
        // Requisitos mínimos
        if (projectilePrefab == null || projectileSpawnPoint == null) return;

        //Cooldown
        if (Time.time - lastShotTime < shotCooldown) return;

        lastShotTime = Time.time;

        // Anim opcional
        animator?.SetTrigger(shootTriggerName);

        StartCoroutine(RangedShotRoutine());
    }

    void Move()
    {
        if (IsAttacking()) return;

        float currentSpeed = isBlocking ? blockSpeed : speed;

        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
        float magnitude = dir.magnitude;
        if (dir.magnitude > 1f) dir.Normalize();

        animator.SetFloat("Speed", magnitude);

        CheckIsGrouded(dir, currentSpeed);
    }

    private void CheckIsGrouded(Vector3 horizontalDir, float currentSpeed)
    {
        // Aplicar gravedad
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += -9.81f * Time.deltaTime;
        }

        // Combina movimiento horizontal + vertical
        Vector3 horizontalVelocity = horizontalDir * currentSpeed;

        // Movimiento final
        Vector3 finalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        // Mueve el CharacterController
        controller.Move(finalVelocity * Time.deltaTime);
    }

    void LookAtMouse()
    {
        Ray ray = cam.ScreenPointToRay(mouseScreenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
            return;

        Vector3 dir = hit.point - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        RotateTowards(dir);
    }

    void LookWithStick()
    {
        if (stickInputPos.sqrMagnitude < gamepadStickDeadzone * gamepadStickDeadzone) return;

        Vector3 dir = new Vector3(stickInputPos.x, 0f, stickInputPos.y);
        RotateTowards(dir);
    }

    void RotateTowards(Vector3 dir)
    {
        dir.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    bool IsAttacking()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    }

    private void SpawnMeleeVfx(int combo)
    {
        if (meleeVfxPrefabs == null || meleeVfxPrefabs.Count == 0) return;

        int index = Mathf.Clamp(combo - 1, 0, meleeVfxPrefabs.Count - 1);
        GameObject selectedVfx = meleeVfxPrefabs[index];
        if (selectedVfx == null) return;

        Transform spawnPoint = meleeVfxSpawnPoint != null ? meleeVfxSpawnPoint : transform;
        Instantiate(selectedVfx, spawnPoint);
    }

    private void ResetFatigue()
    {
        isFatigued = false;
        comboStep = 0;
        //animator.SetTrigger("Recovered");
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        lastShieldHitTime = Time.time;

        ResetRegenShieldCoroutine();

        // Bloqueando con escudo activo
        if (isBlocking && shieldActive)
        {
            currentShieldHits++;
            playerInfoGUI?.UpdateShields(currentShieldHits);

            if (currentShieldHits >= maxShieldHits)
            {
                BreakShield();
                ApplyDamage(damage * 2f);
            }

            return;
        }

        ApplyDamage(damage);
    }

    void ApplyDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        playerInfoGUI?.UpdateHealth(currentHealth, maxHealth);

        animator.SetTrigger("TakeDamage");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void BreakShield()
    {
        shieldActive = false;

        foreach(GameObject shield in shieldAura){
            shield?.SetActive(false);
        }
    }

    IEnumerator RegenShield()
    {
        yield return new WaitForSeconds(shieldRegenTime);

        // Si durante la espera hubo otro golpe, no regenera
        if (Time.time - lastShieldHitTime < shieldRegenTime)
            yield break;

        shieldActive = true;
        currentShieldHits = 0;
        playerInfoGUI?.UpdateShields(currentShieldHits);

        foreach (GameObject shield in shieldAura)
        {
            shield?.SetActive(false);
        }
        shieldAura[0]?.SetActive(isBlocking);

        shieldRegenRoutine = null;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentHealth = 0f;

        if (meleeHitbox != null) 
            meleeHitbox.SetActive(false);

        animator.SetBool("IsDead", true);

        if (isFailureCondition())
            StartCoroutine(ShowFailureDelayed());
    }

    private void UpdateShieldVisual()
    {
        foreach (GameObject shield in shieldAura)
        {
            shield?.SetActive(false);
        }

        if (!shieldActive || currentShieldHits == maxShieldHits) return;

        shieldAura[currentShieldHits]?.SetActive(isBlocking);
    }

    private void ResetRegenShieldCoroutine()
    {
        if (shieldRegenRoutine != null)
        {
            StopCoroutine(shieldRegenRoutine);
        }
        shieldRegenRoutine = StartCoroutine(RegenShield());
    }

    IEnumerator MeleeAttackRoutine(int combo)
    {
        isAttackPerforming = true;

        float windup = attackWindup;
        float activeTime = attackActiveTime;

        // Espera antes del impacto
        yield return new WaitForSeconds(windup);

        // Activar VFX y hitbox
        SpawnMeleeVfx(combo);
        meleeHitbox?.SetActive(true);

        // Ventana activa
        yield return new WaitForSeconds(activeTime);

        // Desactivar hitbox
        meleeHitbox?.SetActive(false);

        // Pequeño cooldown técnico
        yield return new WaitForSeconds(attackCooldown);

        isAttackPerforming = false;
    }

    IEnumerator RangedShotRoutine()
    {
        isAttackPerforming = true;

        if (shotWindup > 0f)
            yield return new WaitForSeconds(shotWindup);

        FireProjectile();

        // “Lock” pequeño para que no spamee el input en el mismo frame
        yield return null;
        isAttackPerforming = false;
    }

    private void FireProjectile()
    {
        Vector3 dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        GameObject go = Instantiate(projectilePrefab, projectileSpawnPoint.position, rot);

        // Si el proyectil tiene script, pásale daño/velocidad/dirección
        var proj = go.GetComponent<PlayerProjectile>();
        if (proj != null)
        {
            proj.Init(dir.normalized, projectileSpeed, damage);
            return;
        }

        // Fallback: si lleva Rigidbody, empújalo
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = dir.normalized * projectileSpeed;
        }
    }

    public void SetPlayerInfoGUI(GUIPlayerInfo panel)
    {
        playerInfoGUI = panel;
    }

    private bool isFailureCondition()
    {
        MovementController[] players = FindObjectsByType<MovementController>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            if (!player.isDead)
                return false;
        }

        return true;
    }

    private IEnumerator ShowFailureDelayed()
    {
        yield return new WaitForSeconds(2f);

        GUIManager.Instance.ShowFailurePopup();
    }

}
