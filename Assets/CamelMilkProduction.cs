using UnityEngine;

public class CamelMilkProduction : MonoBehaviour
{
    public float productionTime = 10f;
    public int milkAmount = 1;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= productionTime)
        {
            ProduceMilk();
            timer = 0f;
        }
    }

    void ProduceMilk()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMilk(milkAmount);

            MilkManager milkManager = FindFirstObjectByType<MilkManager>();

            if (milkManager != null)
            {
                milkManager.UpdateMilkText();
            }
            else
            {
                Debug.LogWarning("MilkManager غير موجود في المشهد!");
            }

            Debug.Log("الناقة أنتجت حليب 🥛");
        }
        else
        {
            Debug.LogWarning("GameManager غير موجود!");
        }
    }
}