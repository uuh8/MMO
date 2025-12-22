using Managers;
using Models;
using Services;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGuild : UIWindow
{
    public GameObject guildItemPrefab;
    public ListView listMain;
    public Transform itemRoot;      // 在哪个根节点创建列表
    public UIGuildInfo uiInfo;
    public UIGuildMemberItem selectedItem;

    public GameObject panelAdmin;   // 管理者面板
    public GameObject panelLeader;  // 会长面板

    void Start()
    {
        GuildService.Instance.OnGuildUpdate += UpdateUI;  // 注册事件，网络有新消息的时候刷新UI
        this.listMain.onItemSelected += this.OnGuildMemberSelected;
        UpdateUI();    // 启动时自动刷新一次UI
    }
    void OnDestroy()
    {
        GuildService.Instance.OnGuildUpdate -= UpdateUI;
    }

    public void OnGuildMemberSelected(ListView.ListViewItem item)
    {
        this.selectedItem = item as UIGuildMemberItem;
    }

    private void UpdateUI()
    {
        this.uiInfo.Info = GuildManager.Instance.guildInfo;

        ClearGuildList();    // 清空主线/支线两个列表
        InitGuildItems();    // 重建两个列表
    }

    private void InitGuildItems()
    {
        foreach (var Item in GuildManager.Instance.guildInfo.Members)
        {
            GameObject go = Instantiate(guildItemPrefab, this.listMain.transform);
            UIGuildMemberItem uiItem = go.GetComponent<UIGuildMemberItem>();
            uiItem.SetGuildMemberInfo(Item);
            this.listMain.AddItem(uiItem);
        }
    }
    private void ClearGuildList()
    {
        this.listMain.RemoveAll();
    }

    #region 工会界面按钮点击
    /// <summary>
    /// 点击离开公会按钮
    /// </summary>
    /// <param name="input"></param>
    /// <param name="tips"></param>
    /// <returns></returns>
    private bool OnClickLeave(string input, out string tips)
    {
        tips = "";
        int friendId = 0;
        string friendName = "";
        if (!int.TryParse(input, out friendId))
            friendName = input;
        if (friendId == User.Instance.CurrentCharacter.Id || friendName == User.Instance.CurrentCharacter.Name)
        {
            tips = "不能添加自己为好友";
            return false;
        }

        FriendService.Instance.SendFriendAddRequest(friendId, friendName);
        return true;
    }
    /// <summary>
    /// 点击私聊按钮
    /// </summary>
    public void OnClickChat()
    {
        MessageBox.Show("暂未开放");
    }
    /// <summary>
    /// 点击踢人按钮
    /// </summary>
    public void OnClickKickout()
    {
        if (selectedItem == null)
        {
            MessageBox.Show("请选择要删除的公会成员");
            return;
        }
        MessageBox.Show(string.Format("确定要删除好友[{0}]吗？", selectedItem.memberInfo.Info.Name), "删除好友", MessageBoxType.Confirm, "删除", "取消").OnYes = () =>
        {
            FriendService.Instance.SendFriendRemoveRequest(this.selectedItem.memberInfo.Id, this.selectedItem.memberInfo.Info.Id);
        };
    }
    /// <summary>
    /// 点击好友组队按钮
    /// </summary>
    public void OnClickPromote()
    {
        // 如未选中好友
        if (selectedItem == null)
        {
            MessageBox.Show("请选择要邀请的好友");
            return;
        }
        // 若是离线好友
        if (selectedItem.memberInfo.Status == 0)
        {
            MessageBox.Show("请选择在线的好友");
            return;
        }

        MessageBox.Show(string.Format("确定要邀请好友【{0}】加入队伍吗？", selectedItem.memberInfo.Info.Name), "邀请好友组队", MessageBoxType.Confirm, "邀请", "取消").OnYes = () =>
        {
            TeamService.Instance.SendTeamInviteRequest(this.selectedItem.memberInfo.Info.Id, this.selectedItem.memberInfo.Info.Name);
        };
    }

    public void OnClickDepose()
    {

    }
    public void OnClickTransfer()
    {

    }
    public void OnClickSetNotice()
    {

    }
    #endregion

}
