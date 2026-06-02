using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MinimapLayerHider
{
    static MinimapLayerHider()
    {
        HideMinimapLayer();
    }

    private static void HideMinimapLayer()
    {
        int layerIndex = LayerMask.NameToLayer("MinimapUI");

        if (layerIndex != -1)
        {
            Tools.visibleLayers &= ~(1 << layerIndex);
        }
    }
}