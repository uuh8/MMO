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
    public void OnClickLeave()
    {
        /*tips = "";
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
        return true;*/
    }
    /// <summary>
    /// 点击私聊按钮
    /// </summary>
    /// <param name="input"></param>
    /// <param name="tips"></param>
    /// <returns></returns>
    public void OnClickChat()
    {
        /*tips = "";
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
        return true;*/
    }
    /// <summary>
    /// 点击公会宣言按钮
    /// </summary>
    /// <param name="input"></param>
    /// <param name="tips"></param>
    /// <returns></returns>
    public void OnClickNotice()
    {
        /*tips = "";
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
        return true;*/
    }

    /// <summary>
    /// 点击踢人按钮
    /// </summary>
    public void OnClickKickout()
    {
        if(selectedItem == null)
        {
            MessageBox.Show("请选择要踢出的成员");
            return;
        }
        MessageBox.Show(
            string.Format("要踢[{0}]出公会吗？", this.selectedItem.memberInfo.Info.Name),
            "踢出公会",
            MessageBoxType.Confirm, 
            "确定", 
            "取消").OnYes = () =>
            {
                GuildService.Instance.SendAdminCommand(GuildAdminCommand.Kickout, this.selectedItem.memberInfo.Info.Id);
            };
    }
    /// <summary>
    /// 点击申请列表按钮
    /// </summary>
    public void OnClickGuildApplyList()
    {
        UIManager.Instance.Show<UIGuildApplyList>();
    }
    /// <summary>
    /// 点击晋升按钮
    /// </summary>
    public void OnClickPromote()
    {
        if (selectedItem == null)
        {
            MessageBox.Show("请选择要晋升的成员");
            return;
        }
        if(selectedItem.memberInfo.Title != GuildTitle.None)
        {
            MessageBox.Show("对方已经身份尊贵");
            return;
        }
        MessageBox.Show(
            string.Format("要晋升[{0}]为公会副会长吗？", this.selectedItem.memberInfo.Info.Name),
            "晋升",
            MessageBoxType.Confirm,
            "确定",
            "取消").OnYes = () =>
            {
                GuildService.Instance.SendAdminCommand(GuildAdminCommand.Promote, this.selectedItem.memberInfo.Info.Id);
            };
    }
    /// <summary>
    /// 点击罢免按钮
    /// </summary>
    public void OnClickDepose()
    {
        if (selectedItem == null)
        {
            MessageBox.Show("请选择要罢免的成员");
            return;
        }
        if (selectedItem.memberInfo.Title == GuildTitle.None)
        {
            MessageBox.Show("对方似乎无职可免");
            return;
        }
        if (selectedItem.memberInfo.Title == GuildTitle.President)
        {
            MessageBox.Show("会长不是你能动的");
            return;
        }
        MessageBox.Show(
            string.Format("确认要罢免[{0}]的公会职务吗？", this.selectedItem.memberInfo.Info.Name),
            "职务罢免",
            MessageBoxType.Confirm,
            "确定",
            "取消").OnYes = () =>
            {
                GuildService.Instance.SendAdminCommand(GuildAdminCommand.Depost, this.selectedItem.memberInfo.Info.Id);
            };
    }

    /// <summary>
    /// 点击转让按钮
    /// </summary>
    public void OnClickTransfer()
    {
        if (selectedItem == null)
        {
            MessageBox.Show("请选择把会长转让给的成员");
            return;
        }

        MessageBox.Show(
            string.Format("要把会长转让给[{0}]吗？", this.selectedItem.memberInfo.Info.Name),
            "会长转让",
            MessageBoxType.Confirm,
            "确定",
            "取消").OnYes = () =>
            {
                GuildService.Instance.SendAdminCommand(GuildAdminCommand.Transfer, this.selectedItem.memberInfo.Info.Id);
            };
    }
    #endregion

}
