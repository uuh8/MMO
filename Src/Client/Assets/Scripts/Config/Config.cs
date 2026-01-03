using UnityEngine;

public static class Config
{
    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt("Music", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("Music", value ? 1 : 0);
            if (Application.isPlaying)
                SoundManager.Instance.MusicOn = value;
        }
    }

    public static bool SoundOn
    {
        get => PlayerPrefs.GetInt("Sound", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("Sound", value ? 1 : 0);
            if (Application.isPlaying)
                SoundManager.Instance.SoundOn = value;
        }
    }

    public static int MusicVolume
    {
        get => PlayerPrefs.GetInt("MusicVolume", 100);
        set
        {
            int v = Mathf.Clamp(value, 0, 100);
            PlayerPrefs.SetInt("MusicVolume", v);
            if (Application.isPlaying)
                SoundManager.Instance.MusicVolume = v;
        }
    }

    public static int SoundVolume
    {
        get => PlayerPrefs.GetInt("SoundVolume", 100);
        set
        {
            int v = Mathf.Clamp(value, 0, 100);
            PlayerPrefs.SetInt("SoundVolume", v);
            if (Application.isPlaying)
                SoundManager.Instance.SoundVolume = v;
        }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
