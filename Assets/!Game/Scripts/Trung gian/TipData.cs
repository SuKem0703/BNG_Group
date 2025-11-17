using UnityEngine;
using System.Collections.Generic;

public static class TipData
{
    private static readonly List<string> TipsList = new()
    {
        "Càng đi xa, ta càng quên mất lý do mình bắt đầu.",
        "Sức mạnh không đến từ kiếm hay phép, mà từ ý chí sống sót.",
        "Trong bóng tối, người dám thắp lửa sẽ trở thành hy vọng.",
        "Không ai sinh ra là anh hùng. Nhưng ai cũng có lựa chọn.",
        "Ký ức là thứ ma thuật mạnh nhất — và cũng nguy hiểm nhất.",
    };

    public static string GetRandomTip()
    {
        if (TipsList.Count == 0)
        {
            return "Đang tải...";
        }

        int index = Random.Range(0, TipsList.Count);
        return TipsList[index];
    }
}