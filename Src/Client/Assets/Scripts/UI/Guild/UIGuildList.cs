using Managers;
using Models;
using Services;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGuildList : UIWindow
{
    public GameObject guildItemPrefab;
    public ListView listMain;
    public Transform itemRoot;      // 在哪个根节点创建列表
    public UIGuildInfo uiInfo;
    public UIGuildItem selectedItem;

    void Start()
    {
        this.listMain.onItemSelected += this.OnGuildMemberSelected;
        this.uiInfo.Info = null;
        GuildService.Instance.OnGuildListResult += UpdateGuildList; // 收到服务端消息就刷新ui

        GuildService.Instance.SendGuildListRequest();   // 刷新
    }
    private void OnDestroy()
    {
        GuildService.Instance.OnGuildListResult -= UpdateGuildList; 
    }
    /// <summary>
    /// 更新ui
    /// </summary>
    /// <param name="guilds"></param>
    void UpdateGuildList(List<NGuildInfo> guilds)
    {
        ClearGuildList();
        InitGuildItems(guilds);
    }
    /// <summary>
    /// 选中某个工会
    /// </summary>
    /// <param name="item"></param>
    public void OnGuildMemberSelected(ListView.ListViewItem item)
    {
        this.selectedItem = item as UIGuildItem;
        this.uiInfo.Info = this.selectedItem.Info;
    }
    /// <summary>
    /// 初始化工会item
    /// </summary>
    /// <param name="guilds"></param>
    private void InitGuildItems(List<NGuildInfo> guilds)
    {
        foreach (var Item in guilds)
        {
            GameObject go = Instantiate(guildItemPrefab, this.listMain.transform, false);
            UIGuildItem uiItem = go.GetComponent<UIGuildItem>();
            uiItem.SetGuildInfo(Item);
            this.listMain.AddItem(uiItem);
        }
    }
    /// <summary>
    /// 清理工会列表
    /// </summary>
    private void ClearGuildList()
    {
        this.listMain.RemoveAll();
    }

    /// <summary>
    /// 点击“申请加入”按钮
    /// </summary>
    public void OnClickJoin()
    {
        if(selectedItem == null)
        {
            MessageBox.Show("请选择要加入的工会");
            return;
        }
        MessageBox.Show(string.Format("确定要加入工会【{0}】吗？", selectedItem.Info.GuildName), "申请加入工会", MessageBoxType.Confirm, "确认", "取消").OnYes = () =>
        {
            GuildService.Instance.SendGuildJoinRequest(this.selectedItem.Info.Id);
        };
    }

}
