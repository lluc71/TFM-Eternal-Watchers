using System.Collections;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

public class EnemyRogue : EnemyBasic
{
    private static readonly int CamoAmountId = Shader.PropertyToID("_CamoAmount");

    [Header("Rogue Target Priority")]
    [Range(0f, 1f)] public float lowHealthWeight = 0.7f; // 0 = solo distancia, 1 = solo baja vida


    [Header("Rogue Camouflage")]
    [Range(0f, 1f)] public float chaseCamo = 0.75f;
    public float fadeDuration = 0.2f;

    [Tooltip("Si lo dejas vacío, buscará todos los Renderers hijos.")]
    public Renderer[] targetRenderers;

    private MaterialPropertyBlock mpb;
    private Coroutine fadeRoutine;
    private float currentCamo = 0f;

    protected override void Start()
    {
        base.Start();

        mpb = new MaterialPropertyBlock();

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        ApplyCamo(0f);
        SetCastShadows(true);
    }

    // -- Prioriza el Target por su Vida y no solo por la Distancia
    protected override Transform ChooseTarget()
    {
        var players = PlayerRegistry.Players;
        if (players == null || players.Count == 0)
            return null;

        float maxDistSqr = GetTargetingRangeSqr();

        Transform bestTarget = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < players.Count; i++)
        {
            var candidate = players[i];
            if (!candidate) continue;

            if (!IsCandidateInRange(candidate, maxDistSqr))
                continue;

            float score = EvaluateTargetScore(candidate, maxDistSqr);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        return bestTarget != null ? bestTarget : base.ChooseTarget();
    }

    protected override void OnStateChanged(EnemyState previous, EnemyState next)
    {
        base.OnStateChanged(previous, next);

        if (next == EnemyState.Chase)
        {
            FadeTo(chaseCamo);
            SetCastShadows(false);
        }

        if (previous == EnemyState.Chase && next != EnemyState.Chase)
        {
            FadeTo(0f);
            SetCastShadows(true);
        }

        if (next == EnemyState.Dead)
        {
            FadeTo(0f);
            SetCastShadows(true);
        }
    }

    void FadeTo(float target)
    {
        if (Mathf.Abs(currentCamo - target) < 0.01f) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(target, fadeDuration));
    }


    IEnumerator FadeRoutine(float target, float duration)
    {
        float start = currentCamo;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            ApplyCamo(Mathf.Lerp(start, target, t / duration));
            yield return null;
        }

        ApplyCamo(target);
        fadeRoutine = null;
    }

    void ApplyCamo(float value)
    {
        currentCamo = value;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (!r) continue;

            r.GetPropertyBlock(mpb);
            mpb.SetFloat(CamoAmountId, value);
            r.SetPropertyBlock(mpb);
        }
    }

    void SetCastShadows(bool enabled)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (!r) continue;

            r.shadowCastingMode = enabled
                ? ShadowCastingMode.On
                : ShadowCastingMode.Off;
        }
    }

    float GetTargetingRangeSqr()
    {
        float range = (state == EnemyState.Idle || state == EnemyState.Patrol)
            ? detectionRange
            : loseTargetRange;

        return range * range;
    }

    bool IsCandidateInRange(Transform candidate, float maxDistSqr)
    {
        float distSqr = (candidate.position - transform.position).sqrMagnitude;
        return distSqr <= maxDistSqr;
    }

    float EvaluateTargetScore(Transform candidate, float maxDistSqr)
    {
        float distSqr = (candidate.position - transform.position).sqrMagnitude;

        float distanceScore = GetDistanceScore(distSqr, maxDistSqr);
        float lowHealthScore = GetLowHealthScore(candidate);

        float score = Mathf.Lerp(distanceScore, lowHealthScore, lowHealthWeight);

        // Sticky bonus: evita cambios por micro variaciones
        if (candidate == playerTarget)
            score += 0.05f;

        return score;
    }

    float GetDistanceScore(float distSqr, float maxDistSqr)
    {
        return 1f - Mathf.Clamp01(distSqr / maxDistSqr);
    }

    float GetLowHealthScore(Transform candidate)
    {
        var hp = candidate.GetComponentInParent<MovementController>();
        if (hp == null || hp.maxHealth <= 0f)
            return 0f;

        float health01 = Mathf.Clamp01(hp.currentHealth / hp.maxHealth);
        return 1f - health01; // 1 = casi muerto
    }

}
