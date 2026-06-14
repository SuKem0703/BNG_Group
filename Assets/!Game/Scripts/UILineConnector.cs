using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(CanvasRenderer))]
public class UILineConnector : MaskableGraphic
{
    [System.Serializable]
    public struct SkillConnection
    {
        public RectTransform parentNode;
        public RectTransform childNode;
    }

    public List<SkillConnection> connections = new List<SkillConnection>();
    public float lineWidth = 4f;
    public Color lineColor = Color.gray;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (connections == null || connections.Count == 0) return;

        foreach (var connection in connections)
        {
            if (connection.parentNode == null || connection.childNode == null) continue;

            Vector2 startPos = GetLocalPositionInConnector(connection.parentNode);
            Vector2 endPos = GetLocalPositionInConnector(connection.childNode);

            DrawLine(startPos, endPos, vh);
        }
    }

    private Vector2 GetLocalPositionInConnector(RectTransform target)
    {
        Vector3 worldPos = target.TransformPoint(Vector3.zero);
        return rectTransform.InverseTransformPoint(worldPos);
    }

    private void DrawLine(Vector2 start, Vector2 end, VertexHelper vh)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * (lineWidth / 2f);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = lineColor;

        int currentVertCount = vh.currentVertCount;

        vertex.position = start - normal;
        vh.AddVert(vertex);

        vertex.position = start + normal;
        vh.AddVert(vertex);

        vertex.position = end + normal;
        vh.AddVert(vertex);

        vertex.position = end - normal;
        vh.AddVert(vertex);

        vh.AddTriangle(currentVertCount, currentVertCount + 1, currentVertCount + 2);
        vh.AddTriangle(currentVertCount, currentVertCount + 2, currentVertCount + 3);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            SetAllDirty();
        }
    }
}