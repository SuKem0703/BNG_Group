using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveSetting
{
    public float sfxVolume = 1.0f;
    public float bgmVolume = 1.0f;
    public int graphicsLevel = 2;
    public float lightIntensity = 1.0f;

    public bool fxaaEnabled = true;
    public bool isFullScreen = true;

    public string language;

    public float cameraZoom = 5.0f;
}