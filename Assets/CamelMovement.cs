using UnityEngine;

public class CamelMovement : MonoBehaviour
{
    public float moveSpeed = 0.5f;
    public float moveRange = 1.2f;
    public float waitTime = 2f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float waitTimer;
    private Camel camel;

    void Start()
    {
        startPosition = transform.position;
        camel = GetComponent<Camel>();
        ChooseNewTarget();
    }

    void Update()
    {
        // توقف إذا الحليب جاهز (لين يتجمع) أو وقت التركيب
        if (ShouldStop()) return;

        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            waitTimer = waitTime;
            ChooseNewTarget();
        }
    }

    bool ShouldStop()
    {
        if (PlacementManager.IsPlacing) return true;

        if (camel != null && camel.milkReadyIcon != null && camel.milkReadyIcon.activeSelf)
            return true;

        return false;
    }

    void ChooseNewTarget()
    {
        float x = Random.Range(-moveRange, moveRange);
        float z = Random.Range(-moveRange, moveRange);

        targetPosition = startPosition + new Vector3(x, 0, z);
    }
}