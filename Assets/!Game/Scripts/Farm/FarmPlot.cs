using UnityEngine;

public class FarmPlot : AutoIDBehaviour
{
    public bool isPlanted = false;
    public Crop currentCrop;

    private void Start()
    {
        if (FarmController.Instance != null)
        {
            FarmController.Instance.RegisterPlot(this);
        }
    }

    private void OnDestroy()
    {
        if (FarmController.Instance != null)
        {
            FarmController.Instance.UnregisterPlot(this);
        }
    }
}