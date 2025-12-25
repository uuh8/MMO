using Services;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGuildApplyItem : ListView.ListViewItem
{
    public Text nickname;
    public Text @class;
    public Text level;

    public NGuildApplyInfo applyInfo;

    public void SetApplyItemInfo(NGuildApplyInfo item)
    {
        this.applyInfo = item;
        if (this.nickname != null) this.nickname.text = this.applyInfo.Name;
        if (this.@class != null) this.@class.text = this.applyInfo.Class.ToString();
        if (this.level != null) this.level.text = this.applyInfo.Level.ToString();
    }

    /// <summary>
    /// 绑定给按钮
    /// </summary>
    public void OnAccept()
    {
        MessageBox.Show(
            string.Format("要通过[{0}]的公会加入申请吗？", this.applyInfo.Name),
            "审批申请",
            MessageBoxType.Confirm,
            "同意加入",
            "取消").OnYes = () =>
            {
                GuildService.Instance.SendGuildJoinApply(true, this.applyInfo);
            };
    }
    /// <summary>
    /// 绑定给按钮
    /// </summary>
    public void OnDecline()
    {
        MessageBox.Show(
            string.Format("要拒绝[{0}]的公会加入申请吗？", this.applyInfo.Name),
            "审批申请",
            MessageBoxType.Confirm,
            "拒绝加入",
            "取消").OnYes = () =>
            {
                GuildService.Instance.SendGuildJoinApply(false, this.applyInfo);
            };
    }
}
