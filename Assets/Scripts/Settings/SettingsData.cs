using System;

[System.Serializable]
public class SettingsData
{
    public float masterVolume = 1f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 0.9f;
    public float sensitivity = 1f;

    public int qualityLevel = 2;
    public bool fullscreen = true;
    public bool invertY = false;

}