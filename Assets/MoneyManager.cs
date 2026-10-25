using UnityEngine;
using RTLTMPro;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;
    public RTLTextMeshPro moneyText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateMoneyText();
    }

    public void UpdateMoneyText()
    {
        if (moneyText == null)
        {
            Debug.LogError("MoneyText غير مربوط في Inspector!", this);
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance غير موجود!");
            return;
        }

        moneyText.text = ToArabicNumbers(GameManager.Instance.money);
        moneyText.ForceMeshUpdate();
    }

    private string ToArabicNumbers(int number)
    {
        char[] chars = number.ToString().ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('\u0660' + (chars[i] - '0'));
        }

        return new string(chars);
    }
}