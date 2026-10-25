using UnityEngine;
using System.Collections;

public class CamelAnimator : MonoBehaviour
{
    [Header("Walk")]
    public float walkSpeed = 8f;
    public float walkTilt = 3f;
    public float walkBob = 0.04f;

    [Header("Eat")]
    public float eatAngle = 15f;
    public float eatSquash = 0.08f;
    public float eatDuration = 2f;

    private Quaternion baseRotation;
    private Vector3 baseScale;
    private Vector3 lastPosition;
    private CamelMovement movement;

    private bool isEating = false;
    private float walkTime = 0f;
    private float currentAngle = 0f;
    private float currentStretch = 1f;

    void Start()
    {
        baseRotation = transform.localRotation;
        baseScale = transform.localScale;
        lastPosition = transform.position;
        movement = GetComponent<CamelMovement>();
    }

    void LateUpdate()
    {
        if (isEating)
        {
            lastPosition = transform.position;
            return;
        }

        bool isMoving = (transform.position - lastPosition).sqrMagnitude > 0.0000001f;
        lastPosition = transform.position;

        if (isMoving)
        {
            walkTime += Time.deltaTime * walkSpeed;
            currentAngle = Mathf.Sin(walkTime) * walkTilt;
            currentStretch = 1f + Mathf.Abs(Mathf.Sin(walkTime)) * walkBob;
        }
        else
        {
            currentAngle = Mathf.Lerp(currentAngle, 0f, Time.deltaTime * 8f);
            currentStretch = Mathf.Lerp(currentStretch, 1f, Time.deltaTime * 8f);
        }

        Apply();
    }

    public void PlayEat()
    {
        if (isEating) return;
        StartCoroutine(EatRoutine());
    }

    IEnumerator EatRoutine()
    {
        isEating = true;
        if (movement != null) movement.enabled = false;

        float moveTime = 0.35f;
        float startAngle = currentAngle;
        float startStretch = currentStretch;

        float t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moveTime);
            currentAngle = Mathf.Lerp(startAngle, eatAngle, k);
            currentStretch = Mathf.Lerp(startStretch, 1f - eatSquash, k);
            Apply();
            yield return null;
        }

        float chewTime = Mathf.Max(0f, eatDuration - moveTime * 2f);
        t = 0f;
        while (t < chewTime)
        {
            t += Time.deltaTime;
            currentAngle = eatAngle + Mathf.Sin(t * 10f) * 3f;
            currentStretch = 1f - eatSquash;
            Apply();
            yield return null;
        }

        startAngle = currentAngle;
        t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moveTime);
            currentAngle = Mathf.Lerp(startAngle, 0f, k);
            currentStretch = Mathf.Lerp(1f - eatSquash, 1f, k);
            Apply();
            yield return null;
        }

        currentAngle = 0f;
        currentStretch = 1f;
        Apply();

        if (movement != null) movement.enabled = true;
        isEating = false;
    }

    void Apply()
    {
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, currentAngle);
        transform.localScale = new Vector3(baseScale.x, baseScale.y * currentStretch, baseScale.z);
    }

    void OnDisable()
    {
        if (isEating && movement != null) movement.enabled = true;
        isEating = false;
    }

    [ContextMenu("Test Eat")]
    void TestEat()
    {
        PlayEat();
    }
}