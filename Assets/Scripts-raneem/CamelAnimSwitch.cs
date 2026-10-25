using UnityEngine;

public class CamelAnimSwitch : MonoBehaviour
{
    Animator anim;
    Vector3 lastPos;
    float stillTime;
    float baseScaleX;

    void Start()
    {
        anim = GetComponent<Animator>();
        lastPos = transform.position;
        baseScaleX = Mathf.Abs(transform.localScale.x);
    }

    void Update()
    {
        Vector3 delta = transform.position - lastPos;
        lastPos = transform.position;

        if (delta.sqrMagnitude > 0.0000001f) stillTime = 0f;
        else stillTime += Time.deltaTime;

        bool moving = stillTime < 0.15f;
        if (anim != null) anim.SetBool("IsWalking", moving);

        if (Mathf.Abs(delta.x) > 0.0001f)
        {
            Vector3 s = transform.localScale;
            s.x = delta.x > 0 ? baseScaleX : -baseScaleX;
            transform.localScale = s;
        }
    }
}