using UnityEngine;

[DisallowMultipleComponent]
public class TopDownMainCamera : MonoBehaviour
{
    [Header("Targets (1 o 2 jugadores)")]
    [SerializeField] private Transform targetA;
    [SerializeField] private Transform targetB; // opcional

    [Header("Follow")]
    [Tooltip("Radio en el que la cámara NO se mueve (deadzone).")]
    [SerializeField] private float followDeadzone = 2.0f;

    [Tooltip("Suavizado de posición (segundos aprox).")]
    [SerializeField] private float smoothTime = 0.18f;

    [Tooltip("Offset fijo (top-down). Ej: (0, 12, -10)")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 12f, -10f);

    [Header("Zoom (cuando hay 2 jugadores)")]
    [Tooltip("Distancia entre jugadores a partir de la cual empieza a alejarse.")]
    [SerializeField] private float distanceToStartZoom = 4f;

    [Tooltip("Distancia entre jugadores donde llega al zoom máximo.")]
    [SerializeField] private float distanceToMaxZoom = 14f;

    [Tooltip("Cuánto extra se aleja (multiplica el offset) al zoom máximo.")]
    [SerializeField] private float maxZoomMultiplier = 1.35f;

    [Tooltip("Límite duro de zoom (por seguridad). 1 = no alejarse nunca.")]
    [SerializeField] private float hardMaxZoomMultiplier = 1.5f;

    [Header("Limits")]
    [Tooltip("Limita el desplazamiento por frame para evitar tirones (0 = sin límite).")]
    [SerializeField] private float maxMoveSpeed = 0f;

    private Vector3 currentFocus;
    private Vector3 focusVelocity;

    private void Reset()
    {
        // Intenta usar el tag MainCamera si está, si no, nada.
        // (No hace falta.)
    }

    private void Start()
    {
        currentFocus = GetDesiredFocusPoint();
    }

    private void LateUpdate()
    {
        if (!targetA) return;

        Vector3 desiredFocus = GetDesiredFocusPoint();

        // Deadzone: si el foco deseado está dentro del radio, no muevas el foco actual
        Vector3 delta = desiredFocus - currentFocus;
        if (delta.magnitude > followDeadzone)
        {
            Vector3 outside = delta.normalized * (delta.magnitude - followDeadzone);
            Vector3 goal = currentFocus + outside;

            currentFocus = Vector3.SmoothDamp(currentFocus, goal, ref focusVelocity, smoothTime);

            if (maxMoveSpeed > 0f)
            {
                // Limita velocidad real del foco
                focusVelocity = Vector3.ClampMagnitude(focusVelocity, maxMoveSpeed);
            }
        }

        float zoomMul = ComputeZoomMultiplier();

        // Aplica offset y posición final
        Vector3 desiredCamPos = currentFocus + baseOffset * zoomMul;
        transform.position = desiredCamPos;

        // Mantén la rotación fija (si quieres), o comenta si tu cámara ya está orientada como te guste
        // transform.rotation = Quaternion.Euler(45f, 45f, 0f);
    }

    private Vector3 GetDesiredFocusPoint()
    {
        bool aAlive = IsTargetAlive(targetA);
        bool bAlive = IsTargetAlive(targetB);

        if (aAlive && bAlive)
            return (targetA.position + targetB.position) * 0.5f;

        if (aAlive)
            return targetA.position;

        if (bAlive)
            return targetB.position;

        return currentFocus;
    }

    private float ComputeZoomMultiplier()
    {
        if (!targetA || !targetB) return 1f;

        float d = Vector3.Distance(targetA.position, targetB.position);

        // Normaliza distancia en [0..1] para zoom
        float t = Mathf.InverseLerp(distanceToStartZoom, distanceToMaxZoom, d);

        float mul = Mathf.Lerp(1f, maxZoomMultiplier, t);
        return Mathf.Clamp(mul, 1f, hardMaxZoomMultiplier);
    }
    private bool IsTargetAlive(Transform target)
    {
        if (target == null) return false;

        MovementController player = target.GetComponent<MovementController>();
        if (player == null) return false;

        return !player.isDead;
    }

    // API simple para tu spawner / manager
    public void SetTargetA(Transform t) => targetA = t;
    public void SetTargetB(Transform t) => targetB = t;
    public void ClearTargetB() => targetB = null;
}
