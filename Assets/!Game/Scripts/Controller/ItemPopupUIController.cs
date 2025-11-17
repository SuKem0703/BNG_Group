using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPickupUIController : MonoBehaviour
{
    public static ItemPickupUIController Instance { get; private set; }

    public GameObject popupPrefab;
    public int maxPopups = 5;
    public float popupDuration = 2.0f;
    public float popupFadeTime = 1.0f;

    private readonly Queue<GameObject> activePopups = new Queue<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple ItemPickupUIController Instances detected! Destroying the extra one.");
            Destroy(gameObject);
        }
    }

    public void ShowItemPickup(string itemName, Sprite itemIcon, ItemRarity rarity)
    {
        GameObject newPopup = Instantiate(popupPrefab, transform);

        #region Set UI Content
        TMP_Text nameText = newPopup.GetComponentInChildren<TMP_Text>();
        nameText.text = itemName;
        nameText.color = RarityColorHelper.GetColorByRarity(rarity);

        Image itemImage = newPopup.transform.Find("ItemIcon").GetComponent<Image>();
        if (itemImage)
        {
            itemImage.sprite = itemIcon;
        }

        Image borderFrame = newPopup.transform.Find("ItemCard")?.GetComponent<Image>();
        if (borderFrame != null)
        {
            string rarityName = rarity.ToString();
            string path = $"Horizontal Card/{rarityName}";
            Sprite raritySprite = Resources.Load<Sprite>(path);

            if (raritySprite != null)
            {
                borderFrame.sprite = raritySprite;
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy sprite cho rarity '{rarityName}' tại đường dẫn: {path}");
            }
        }

        Image iconCard = newPopup.transform.Find("ItemIconCard")?.GetComponent<Image>();
        if (iconCard != null)
        {
            string rarityName = rarity.ToString();
            string cardPath = $"Square Card/{rarityName}";
            Sprite iconSprite = Resources.Load<Sprite>(cardPath);
            if (iconSprite != null)
            {
                iconCard.sprite = iconSprite;
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy sprite cho item icon tại đường dẫn: {iconCard}");
            }
        }
        #endregion

        ItemPopupLifetime lifetimeScript = newPopup.GetComponent<ItemPopupLifetime>();
        if (lifetimeScript != null)
        {
            lifetimeScript.lifetime = this.popupDuration;
            lifetimeScript.fadeDuration = this.popupFadeTime;
        }
        else
        {
            Debug.LogError("Popup Prefab thiếu script ItemPopupLifetime!");
        }

        activePopups.Enqueue(newPopup);

        if (activePopups.Count > maxPopups)
        {
            GameObject oldestPopup = activePopups.Dequeue();
            if (oldestPopup != null)
            {
                ItemPopupLifetime oldLifetime = oldestPopup.GetComponent<ItemPopupLifetime>();
                if (oldLifetime != null)
                    oldLifetime.StartFadingNow();
                else
                    Destroy(oldestPopup);
            }
        }

    }

    public void ClearAllPopups()
    {
        while (activePopups.Count > 0)
        {
            GameObject popup = activePopups.Dequeue();
            if (popup != null)
                Destroy(popup);
        }
    }
}