#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

public class EquipmentDatabaseImporter
{
    // Thêm nút Import vào menu khi click chuột phải
    [MenuItem("Assets/Import from CSV (Equipment Database)")]
    public static void ImportFromCSV()
    {
        // Kiểm tra file đang chọn có phải EquipmentDatabase không
        EquipmentDatabase db = Selection.activeObject as EquipmentDatabase;

        if (db == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn file EquipmentDatabase.asset!", "OK");
            return;
        }

        // Mở cửa sổ chọn file CSV
        string path = EditorUtility.OpenFilePanel("Chọn file CSV để nạp dữ liệu", "", "csv");

        // Thoát nếu người dùng bấm Cancel
        if (string.IsNullOrEmpty(path)) return;

        // Đọc toàn bộ nội dung file
        string[] lines = File.ReadAllLines(path);

        if (lines.Length <= 1)
        {
            EditorUtility.DisplayDialog("Lỗi", "File CSV trống hoặc chỉ có dòng tiêu đề!", "OK");
            return;
        }

        // Xóa toàn bộ dữ liệu cũ trong Database để nạp cái mới vào
        db.dataRows = new List<EquipmentDataRow>();

        // Chạy vòng lặp từ 1 (để bỏ qua dòng tiêu đề ở số 0)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];

            // Bỏ qua các dòng trống
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Tách dữ liệu bằng dấu phẩy
            string[] values = line.Split(',');

            // Đảm bảo số cột khớp với file xuất ra (8 cột)
            if (values.Length < 8) continue;

            EquipmentDataRow row = new EquipmentDataRow();

            // Ép kiểu các giá trị từ chuỗi (string) sang đúng kiểu dữ liệu
            int.TryParse(values[0], out row.itemId);
            row.itemName = values[1];
            int.TryParse(values[2], out row.reqLevel);

            // Dùng Enum.TryParse để chuyển chuỗi "Knight"/"Mage" thành Enum
            Enum.TryParse(values[3], out row.classRestriction);
            Enum.TryParse(values[4], out row.equipSlot);

            // Ép kiểu float cho 3 giá trị chỉ số
            float.TryParse(values[5], out row.value1);
            float.TryParse(values[6], out row.value2);
            float.TryParse(values[7], out row.value3);

            // Thêm vào Database
            db.dataRows.Add(row);
        }

        // Báo cho Unity biết file này đã bị thay đổi để nó lưu lại
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Thành công", $"Đã nạp thành công {db.dataRows.Count} vật phẩm vào Database!", "OK");
    }

    // Validation để nút Import chỉ sáng lên khi click vào đúng file Database
    [MenuItem("Assets/Import from CSV (Equipment Database)", true)]
    public static bool ImportFromCSVValidation()
    {
        return Selection.activeObject is EquipmentDatabase;
    }
}
#endif