using UnityEngine;

[DisallowMultipleComponent]
public class TopDownMainCamera : MonoBehaviour
{
    [Header("Targets - Jugador A y B")]
    [SerializeField] private Transform targetA;
    [SerializeField] private Transform targetB;

    [Header("Seguimiento")]
    [Tooltip("Distancia en el que la camara NO se mueve.")]
    [SerializeField] private float followDeadzone = 2.0f;

    [Tooltip("Movimiento suavizado (en segundos).")]
    [SerializeField] private float smoothTime = 0.18f;

    [Tooltip("Offset fijo (top-down).")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 12f, -10f);

    [Header("Zoom (cuando hay 2 jugadores)")]
    [Tooltip("Distancia entre jugadores a partir de la cual empieza a alejarse.")]
    [SerializeField] private float distanceToStartZoom = 4f;

    [Tooltip("Distancia entre jugadores donde llega al zoom maximo.")]
    [SerializeField] private float distanceToMaxZoom = 14f;

    [Tooltip("Cuanto extra se aleja al zoom maximo.")]
    [SerializeField] private float maxZoomMultiplier = 1.35f;

    [Tooltip("Limite del zoom (por seguridad). 1 = no alejarse nunca.")]
    [SerializeField] private float hardMaxZoomMultiplier = 1.5f;

    [Header("Optimizacion")]
    [Tooltip("Limita el desplazamiento por frame para evitar tirones (0 = sin límite).")]
    [SerializeField] private float maxMoveSpeed = 0f;

    private Vector3 currentFocus;
    private Vector3 focusVelocity;

    private void Start()
    {
        currentFocus = GetDesiredFocusPoint();
    }

    private void LateUpdate()
    {
        if (!targetA) return;

        Vector3 desiredFocus = GetDesiredFocusPoint();

        // Si el desiredFocus esta dentro del radio, no muevas el currentFocus
        Vector3 delta = desiredFocus - currentFocus;
        if (delta.magnitude > followDeadzone)
        {
            Vector3 outside = delta.normalized * (delta.magnitude - followDeadzone);
            Vector3 goal = currentFocus + outside;

            currentFocus = Vector3.SmoothDamp(currentFocus, goal, ref focusVelocity, smoothTime);

            if (maxMoveSpeed > 0f)
            {
                focusVelocity = Vector3.ClampMagnitude(focusVelocity, maxMoveSpeed);
            }
        }

        transform.position = currentFocus + baseOffset * ComputeZoomMultiplier();
    }

    /**
     * Obtén la posición foco entre los dos jugadores
     */
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
        float t = Mathf.InverseLerp(distanceToStartZoom, distanceToMaxZoom, d);
        float multiplier = Mathf.Lerp(1f, maxZoomMultiplier, t);
        return Mathf.Clamp(multiplier, 1f, hardMaxZoomMultiplier);
    }

    /**
     * Comprueba que el jugador sigue vivo
     */
    private bool IsTargetAlive(Transform target)
    {
        if (target == null) return false;

        MovementController player = target.GetComponent<MovementController>();
        if (player == null) return false;

        return !player.isDead;
    }

    public void SetTargetA(Transform t) => targetA = t;
    public void SetTargetB(Transform t) => targetB = t;
    public void ClearTargetB() => targetB = null;
}
