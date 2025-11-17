using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Slider))]
public class SliderSnap : MonoBehaviour, IPointerUpHandler
{
    private Slider slider;

    [Tooltip("Số phần chia, mặc định 3 nếu <=1")]
    public int divisions = 3;

    private float[] steps;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void Start()
    {
        if (divisions < 2)
            divisions = 3; // mặc định 3 phần

        GenerateSteps();
    }

    void GenerateSteps()
    {
        steps = new float[divisions];
        for (int i = 0; i < divisions; i++)
        {
            steps[i] = i / (float)(divisions - 1); // tạo mốc từ 0 → 1
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        float value = slider.value;
        float closest = steps[0];
        float minDist = Mathf.Abs(value - closest);

        foreach (float step in steps)
        {
            float dist = Mathf.Abs(value - step);
            if (dist < minDist)
            {
                minDist = dist;
                closest = step;
            }
        }

        slider.SetValueWithoutNotify(closest);
    }
}
