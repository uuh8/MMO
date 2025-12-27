using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Models;
using Services;
using Managers;

public class UIMain : MonoSingleton<UIMain>
{
    public Text avatarName;
    public Text avatarLevel;

    public UITeam TeamWindow;   // 组队界面只存在于主UI上，因此直接写在 UIMain 中

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
    /// 点击背包按钮
    /// </summary>
    public void OnClickBag()
    {
        UIManager.Instance.Show<UIBag>();
    }
    /// <summary>
    /// 点击装备按钮
    /// </summary>
    public void OnClickCharEquip()
    {
        UIManager.Instance.Show<UICharEquip>();
    }
    /// <summary>
    /// 点击任务按钮
    /// </summary>
    public void OnClickQuest()
    {
        UIManager.Instance.Show<UIQuestSystem>();
    }
    /// <summary>
    /// 点击好友按钮
    /// </summary>
    public void OnClickFriend()
    {
        UIManager.Instance.Show<UIFriends>();
    }
    /// <summary>
    /// 管理组队界面的 显示/隐藏
    /// </summary>
    /// <param name="show"></param>
    public void ShowTeamUI(bool show)
    {
        TeamWindow.ShowTeam(show);
    }
    /// <summary>
    /// 点击工会按钮
    /// </summary>
    public void OnClickGuild()
    {
        GuildManager.Instance.ShowGuildUI();
    }

    public void OnClickRide()
    {

    }

    public void OnClickSetting()
    {
        UIManager.Instance.Show<UISetting>();
    }

    public void OnClickSkill()
    {

    }
}
