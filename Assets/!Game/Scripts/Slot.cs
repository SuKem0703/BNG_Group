using UnityEngine;

public class Slot : MonoBehaviour
{
    public bool isEquipmentSlot = false;
    public EquipSlot acceptedEquipSlot = EquipSlot.None;
    public ClassRestriction classRestriction = ClassRestriction.None;
    public GameObject currentItem;

    public bool isShopSlot = false;
    public bool isHotBarSlot = false;
}
