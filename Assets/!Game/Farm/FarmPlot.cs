using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    [field: SerializeField] public string PlotID { get; private set; }

    public bool isPlanted = false;
    public Crop currentCrop;

    private void Awake()
    {
        PlotID = GlobalHelper.GenerateUniqueID(gameObject);
    }
}
