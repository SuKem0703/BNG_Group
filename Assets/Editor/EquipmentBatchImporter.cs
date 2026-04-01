#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public class EquipmentBatchImporter : EditorWindow
{
    public EquipmentDatabase database;
    public string prefabFolderPath = "Assets/!Game/Items/Knight/";

    [MenuItem("Tools/Equipment Batch Importer")]
    public static void ShowWindow() => GetWindow<EquipmentBatchImporter>("Batch Importer");

    private void OnGUI()
    {
        database = (EquipmentDatabase)EditorGUILayout.ObjectField("Database Source", database, typeof(EquipmentDatabase), false);
        prefabFolderPath = EditorGUILayout.TextField("Prefab Folder", prefabFolderPath);

        if (GUILayout.Button("IMPORT DATA TO PREFABS") && database != null)
        {
            ApplyDataToPrefabs();
        }
    }

    private void ApplyDataToPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
        int updateCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab.TryGetComponent<EquipmentItem>(out var equipItem))
            {
                var data = database.dataRows.FirstOrDefault(d => d.itemId == equipItem.ID);

                if (data != null && data.itemId != 0)
                {
                    SerializedObject so = new SerializedObject(equipItem);

                    so.FindProperty("Name").stringValue = data.itemName;
                    so.FindProperty("requiredLevel").intValue = data.reqLevel;

                    // Ghi đè luôn Enum Class và Slot từ Database sang Prefab cho đồng bộ 100%
                    so.FindProperty("classRestriction").enumValueIndex = (int)data.classRestriction;
                    so.FindProperty("equipSlot").enumValueIndex = (int)data.equipSlot;

                    // Reset toàn bộ chỉ số rác cũ về 0
                    ResetAllStats(so);

                    float v1 = data.value1;
                    float v2 = data.value2;
                    float v3 = data.value3;

                    // ==== ROUTE DỮ LIỆU TỰ ĐỘNG THEO ENUM ====
                    if (data.classRestriction == ClassRestriction.Knight)
                    {
                        switch (data.equipSlot)
                        {
                            case EquipSlot.Swords:
                                so.FindProperty("physDamageBase").intValue = (int)v1;
                                so.FindProperty("hpKnightBase").intValue = (int)v2;
                                so.FindProperty("strBase").intValue = (int)v3;
                                break;
                            case EquipSlot.Shield:
                                so.FindProperty("defenseBase").intValue = (int)v1;
                                so.FindProperty("dexBase").intValue = (int)v2;
                                so.FindProperty("hpRegenBase").intValue = (int)v3;
                                break;
                            case EquipSlot.Helmet:
                                so.FindProperty("defenseBase").intValue = (int)v1;
                                so.FindProperty("conBase").intValue = (int)v2;
                                so.FindProperty("hpKnightBase").intValue = (int)v3;
                                break;
                            case EquipSlot.Armor:
                                so.FindProperty("defenseBase").intValue = (int)v1;
                                so.FindProperty("dexBase").intValue = (int)v2;
                                so.FindProperty("damageReduction").floatValue = v3; // Ghi thẳng float
                                break;
                        }
                    }
                    else if (data.classRestriction == ClassRestriction.Mage)
                    {
                        switch (data.equipSlot)
                        {
                            case EquipSlot.Scepter:
                                so.FindProperty("magicDamageBase").intValue = (int)v1;
                                so.FindProperty("mpMageBase").intValue = (int)v2;
                                so.FindProperty("intBase").intValue = (int)v3;
                                break;
                            case EquipSlot.Amulet:
                                so.FindProperty("critRateBase").floatValue = v1; // Ghi thẳng float
                                so.FindProperty("conBase").intValue = (int)v2;
                                so.FindProperty("mpRegenBase").intValue = (int)v3;
                                break;
                            case EquipSlot.Hat:
                                so.FindProperty("defenseBase").intValue = (int)v1;
                                so.FindProperty("dexBase").intValue = (int)v2;
                                so.FindProperty("hpMageBase").intValue = (int)v3;
                                break;
                            case EquipSlot.Robe:
                                so.FindProperty("conBase").intValue = (int)v1;
                                so.FindProperty("hpRegenBase").intValue = (int)v2;
                                so.FindProperty("damageReduction").floatValue = v3;
                                break;
                        }
                    }

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(prefab);
                    updateCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Thành công", $"Đã nạp chỉ số cho {updateCount} Prefabs!", "OK");
    }

    private void ResetAllStats(SerializedObject so)
    {
        string[] intStats = { "strBase", "dexBase", "conBase", "intBase",
                              "physDamageBase", "magicDamageBase", "defenseBase",
                              "hpKnightBase", "hpMageBase", "mpKnightBase", "mpMageBase",
                              "hpRegenBase", "mpRegenBase", "staminaRegenBase" };

        foreach (string s in intStats)
            so.FindProperty(s).intValue = 0;

        so.FindProperty("critRateBase").floatValue = 0f;
        so.FindProperty("moveSpeedBase").floatValue = 0f;
        so.FindProperty("damageReduction").floatValue = 0f;
    }
}
#endif