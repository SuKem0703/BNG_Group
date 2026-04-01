#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class EquipmentDatabaseExporter
{
    // Thêm nút bấm vào menu khi click chuột phải vào Asset
    [MenuItem("Assets/Export to CSV (Equipment Database)")]
    public static void ExportToCSV()
    {
        // Lấy file đang được chọn
        EquipmentDatabase db = Selection.activeObject as EquipmentDatabase;

        if (db == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn file EquipmentDatabase.asset!", "OK");
            return;
        }

        // Mở cửa sổ chọn nơi lưu file
        string path = EditorUtility.SaveFilePanel("Lưu file CSV", "", db.name + "_Export.csv", "csv");

        // Nếu người dùng bấm Cancel thì thoát
        if (string.IsNullOrEmpty(path)) return;

        // Dùng StringBuilder để gom dữ liệu
        StringBuilder sb = new StringBuilder();

        // 1. Ghi dòng Tiêu đề (Header)
        sb.AppendLine("ItemID,ItemName,ReqLevel,ClassRestriction,EquipSlot,Value1,Value2,Value3");

        // 2. Duyệt qua từng dòng dữ liệu và ghi vào
        foreach (var row in db.dataRows)
        {
            if (row == null) continue;

            // Xóa dấu phẩy trong tên item (nếu có) để không làm hỏng cấu trúc cột của CSV
            string safeName = string.IsNullOrEmpty(row.itemName) ? "" : row.itemName.Replace(",", " ");

            // Nối các giá trị lại, cách nhau bằng dấu phẩy
            string line = $"{row.itemId},{safeName},{row.reqLevel},{row.classRestriction},{row.equipSlot},{row.value1},{row.value2},{row.value3}";
            sb.AppendLine(line);
        }

        // 3. Ghi ra file vật lý (Sử dụng UTF8 để không bị lỗi font tiếng Việt)
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

        // Hiển thị thông báo thành công
        EditorUtility.DisplayDialog("Thành công", $"Đã xuất file CSV thành công tại:\n{path}", "OK");
    }

    // Hàm này giúp nút "Export" chỉ sáng lên khi bạn đang click vào đúng file EquipmentDatabase
    [MenuItem("Assets/Export to CSV (Equipment Database)", true)]
    public static bool ExportToCSVValidation()
    {
        return Selection.activeObject is EquipmentDatabase;
    }
}
#endif