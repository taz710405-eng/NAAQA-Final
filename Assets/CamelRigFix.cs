using UnityEngine;
using UnityEngine.SceneManagement;

// ===== حركة ابل ٢ =====
// الناقة الثانية في المشهد ناقصها مكوّنات الحركة (العظام والضغط)، فما كانت تتحرك مثل الأولى.
// هذا يبدّلها لحاله بنسخة كاملة من الناقة الأولى، بنفس مكانها وحجمها وعدّادها.
public static class CamelRigFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Fix();
        SceneManager.sceneLoaded += (s, m) => Fix();
    }

    static void Fix()
    {
        OrderManager om = Object.FindAnyObjectByType<OrderManager>(FindObjectsInactive.Include);
        if (om == null || om.newCamel == null) return;

        GameObject old = om.newCamel;
        if (old.GetComponent<CamelClickArea>() != null) return;   // كاملة أصلًا

        Camel first = null;
        foreach (Camel c in Object.FindObjectsByType<Camel>(FindObjectsInactive.Include))
            if (c.gameObject != old && c.GetComponent<CamelClickArea>() != null) { first = c; break; }
        if (first == null) return;

        Camel oldCamel = old.GetComponent<Camel>();
        bool wasActive = old.activeSelf;

        GameObject clone = Object.Instantiate(first.gameObject, old.transform.position, old.transform.rotation, old.transform.parent);
        clone.transform.localScale = old.transform.localScale;
        clone.name = old.name;

        Camel nc = clone.GetComponent<Camel>();
        if (nc != null && oldCamel != null)
        {
            nc.timerBadge = oldCamel.timerBadge;
            nc.timerText = oldCamel.timerText;
            nc.productionTime = oldCamel.productionTime;
        }

        if (PlacementManager.Instance != null && PlacementManager.Instance.blockers != null)
        {
            GameObject[] b = PlacementManager.Instance.blockers;
            for (int i = 0; i < b.Length; i++) if (b[i] == old) b[i] = clone;
        }

        om.newCamel = clone;
        clone.SetActive(wasActive);

        old.name = old.name + "_old";
        old.SetActive(false);
        Object.Destroy(old);
    }
}
