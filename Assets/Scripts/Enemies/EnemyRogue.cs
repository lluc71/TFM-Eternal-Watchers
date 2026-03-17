using System.Collections;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class EnemyRogue : EnemyBasic
{
    private const float ALPHA_OPACO = 1f;

    [Header("Rogue Target Priority")]
    [Range(0f, 1f)] public float lowHealthWeight = 0.7f; // 0 = solo distancia, 1 = solo baja vida


    [Header("Rogue Camouflage")]
    [Range(0f, 1f)] public float chaseAlpha = 0.45f;
    public float fadeDuration = 0.2f;

    [Tooltip("Si lo dejas vacío, buscará todos los Renderers hijos.")]
    public Renderer[] targetRenderers;

    private MaterialPropertyBlock mpb;
    private Coroutine fadeRoutine;
    private float currentAlpha = ALPHA_OPACO;

    protected override void Start()
    {
        base.Start();

        mpb = new MaterialPropertyBlock();

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        Debug.Log($"Renderers encontrados: {targetRenderers.Length}");

        // Arranca opaco
        ApplyAlpha(ALPHA_OPACO);
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

        // Entra en Chase => camuflaje
        if (next == EnemyState.Chase)
            FadeTo(chaseAlpha);

        // Sale de Chase => visible
        if (previous == EnemyState.Chase && next != EnemyState.Chase)
            FadeTo(ALPHA_OPACO);

        // Opcional: al atacar, volver visible (si prefieres)
        // if (next == EnemyState.Attack) FadeTo(1f);

        // Si muere, visible (o lo que prefieras)
        if (next == EnemyState.Dead)
            FadeTo(ALPHA_OPACO);
    }

    void FadeTo(float targetAlpha)
    {
        if (Mathf.Abs(currentAlpha - targetAlpha) < 0.01f) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, fadeDuration));
    }

    IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float start = currentAlpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            ApplyAlpha(Mathf.Lerp(start, targetAlpha, t / duration));
            yield return null;
        }

        ApplyAlpha(targetAlpha);
        fadeRoutine = null;
    }

    void ApplyAlpha(float a)
    {
        currentAlpha = a;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (!r) continue;

            var mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(mpb);

            //Si usara URP no seria _Color seria _BaseColor
            if (mat.HasProperty("_Color"))
            {
                var c = mat.GetColor("_Color");
                c.a = a;
                mpb.SetColor("_Color", c);
            }

            r.SetPropertyBlock(mpb);
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
