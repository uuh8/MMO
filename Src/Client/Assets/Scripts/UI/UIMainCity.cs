using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Models;
using Services;

public class UIMainCity : MonoSingleton<UIMainCity>
{
    public Text avatarName;
    public Text avatarLevel;

    // Start is called before the first frame update
    protected override void OnStart()
    {
        this.UpdateAvatar();
    }

    private void UpdateAvatar()
    {
        this.avatarName.text = string.Format("[UIMainCity] {0}[{1}]", User.Instance.CurrentCharacter.Name, User.Instance.CurrentCharacter.Id);
        this.avatarLevel.text = User.Instance.CurrentCharacter.Level.ToString();
    }

    /// <summary>
    /// 返回角色选择界面（按钮）
    /// </summary>
    public void BackToCharSelect()
    {
        SceneManager.Instance.LoadScene("CharSelect");
        UserService.Instance.SendGameLeave();
    }
}
