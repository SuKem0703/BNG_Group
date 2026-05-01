using UnityEngine;

public class ClassSwapUIAdapter : MonoBehaviour
{
    [Header("Knight UI Groups")]
    public GameObject knightUIGroup;

    [Header("Mage UI Groups")]
    public GameObject mageUIGroup;

    private void Start()
    {
        if (ClassController.Instance != null)
        {
            ClassController.Instance.OnClassSwapped += UpdateUI;

            UpdateUI(ClassController.Instance.GetCurrentClassName());
        }
    }

    private void OnDestroy()
    {
        if (ClassController.Instance != null)
        {
            ClassController.Instance.OnClassSwapped -= UpdateUI;
        }
    }

    private void UpdateUI(string className)
    {
        bool isKnight = (className == "Knight");

        if (knightUIGroup != null) knightUIGroup.SetActive(isKnight);
        if (mageUIGroup != null) mageUIGroup.SetActive(!isKnight);
    }
}