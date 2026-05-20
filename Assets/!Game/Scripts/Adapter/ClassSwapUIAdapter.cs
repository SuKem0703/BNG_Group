using UnityEngine;

public class ClassSwapUIAdapter : MonoBehaviour
{
    private ClassController classController;

    [Header("Knight UI Groups")]
    public GameObject knightUIGroup;

    [Header("Mage UI Groups")]
    public GameObject mageUIGroup;

    private void Start()
    {
        PlayerCore.OnPlayerSpawned += InitializeReference;
    }

    private void InitializeReference(PlayerCore core)
    {
        PlayerCore.OnPlayerSpawned -= InitializeReference;

        classController = core.classController;
        if (classController != null)
        {
            classController.OnClassSwapped += UpdateUI;
            UpdateUI(classController.GetCurrentClassName());
        }
    }

    private void OnDestroy()
    {
        PlayerCore.OnPlayerSpawned -= InitializeReference;

        if (classController != null)
        {
            classController.OnClassSwapped -= UpdateUI;
        }
    }

    private void UpdateUI(string className)
    {
        if (knightUIGroup != null) knightUIGroup.SetActive(className == "Knight");
        if (mageUIGroup != null) mageUIGroup.SetActive(className == "Mage");
    }
}