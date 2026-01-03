using UnityEngine;
using UnityEngine.UI;

public class UISystemConfig : UIWindow
{
    public Image musicOff;
    public Image soundOff;

    public Toggle toggleMusic;
    public Toggle toggleSound;

    public Slider sliderMusic;
    public Slider sliderSound;

    void Start()
    {
        // 注意：初始化 UI 时不要触发 OnValueChanged，
        // 否则会出现“打开界面瞬间把 Config 写坏/把音量写成 0”的问题。
        toggleMusic.SetIsOnWithoutNotify(Config.MusicOn);
        toggleSound.SetIsOnWithoutNotify(Config.SoundOn);

        // Slider 是 0~1，所以要把 Config 的 0~100 转成 0~1
        sliderMusic.SetValueWithoutNotify(Config.MusicVolume / 100f);
        sliderSound.SetValueWithoutNotify(Config.SoundVolume / 100f);

        // 同步“静音图标”的显示状态
        musicOff.enabled = !Config.MusicOn;
        soundOff.enabled = !Config.SoundOn;
    }

    public override void OnYesClick()
    {
        // 点击“关闭/确定”按钮的反馈音
        SoundManager.Instance.PlaySound(SoundDefine.SFX_UI_Click);

        // 显式保存（Config 的析构函数保存是不靠谱的，见 Config.cs 修改）
        Config.Save();

        base.OnYesClick();
    }

    public void MusicToggle(bool on)
    {
        musicOff.enabled = !on;
        Config.MusicOn = on;
        SoundManager.Instance.PlaySound(SoundDefine.SFX_UI_Click);
    }

    public void SoundToggle(bool on)
    {
        soundOff.enabled = !on;
        Config.SoundOn = on;
        SoundManager.Instance.PlaySound(SoundDefine.SFX_UI_Click);
    }

    public void MusicVolume(float vol01)
    {
        // Slider 0~1 -> Config 0~100
        // 关键：不能 (int)vol01，否则永远是 0
        Config.MusicVolume = Mathf.RoundToInt(vol01 * 100f);
        PlaySound();
    }
    public void SoundVolume(float vol01)
    {
        Config.SoundVolume = Mathf.RoundToInt(vol01 * 100f);
        PlaySound();
    }


    private float lastPlay = 0f;

    private void PlaySound()
    {
        // 每次调节给一个提示音，但做节流避免拖动时刷屏/刷声音
        if (TimeUtil.realtimeSinceStartup - lastPlay > 0.1f)
        {
            lastPlay = TimeUtil.realtimeSinceStartup;
            SoundManager.Instance.PlaySound(SoundDefine.SFX_UI_Click);
        }
    }

    // -------------------------
    // Slider 值域兼容（0~1 或 0~100）
    // -------------------------

    private static int SliderValueToVol100(Slider slider, float v)
    {
        // Slider 最大值接近 1：认为是 0~1 归一化
        if (slider.maxValue <= 1.001f) return Mathf.RoundToInt(v * 100f);

        // 否则认为就是 0~100
        return Mathf.RoundToInt(v);
    }

    private static float Vol100ToSliderValue(Slider slider, int vol100)
    {
        vol100 = Mathf.Clamp(vol100, 0, 100);

        if (slider.maxValue <= 1.001f) return vol100 / 100f;
        return vol100;
    }
}
