using UnityEngine;

public class NPCShop : MonoBehaviour
{
    [SerializeField] private GameObject shopUIObject;

    private ShopController shopController;

    private void Awake()
    {
        if (shopUIObject != null)
            shopController = shopUIObject.GetComponent<ShopController>();
    }

    public void OpenShop()
    {
        if (shopUIObject != null)
        {
            shopUIObject.SetActive(true); // 👈 bật object chứa UI
            shopController = shopUIObject.GetComponent<ShopController>(); // gọi lại phòng khi null

            if (shopController != null)
            {
                shopController.OpenShop();
            }
            else
            {
                Debug.LogWarning("ShopController is missing even after enabling shopUIObject!");
            }
        }
    }

}
