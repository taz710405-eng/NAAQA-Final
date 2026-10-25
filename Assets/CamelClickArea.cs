using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CamelClickArea : MonoBehaviour
{
    public float padding = 1.15f;
    public float refreshTime = 0.25f;

    private BoxCollider box;
    private float timer;

    void Start()
    {
        box = GetComponent<BoxCollider>();
        Fit();
    }

    void LateUpdate()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = refreshTime;
        Fit();
    }

    void Fit()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        bool found = false;
        Bounds b = new Bounds();

        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled) continue;
            if (r.gameObject.name == "MilkIcon") continue;

            if (!found) { b = r.bounds; found = true; }
            else b.Encapsulate(r.bounds);
        }

        if (!found) return;

        Vector3 localCenter = transform.InverseTransformPoint(b.center);
        Vector3 localSize = transform.InverseTransformVector(b.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        localSize *= padding;
        localSize.x = Mathf.Max(localSize.x, 0.1f);
        localSize.y = Mathf.Max(localSize.y, 0.1f);
        localSize.z = Mathf.Max(localSize.z, 0.1f);

        box.center = localCenter;
        box.size = localSize;
    }
}