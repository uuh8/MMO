using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGuildMemberItem : ListView.ListViewItem
{
    public Text nickname;
    public Text @class;
    public Text level;
    public Text title;
    public Text joinTime;
    public Text status;

    public Image background;
    public Sprite normalBg;
    public Sprite selectedBg;

    public override void onSelected(bool selected)
    {
        this.background.overrideSprite = selected ? selectedBg : normalBg;
    }

    // 当前的工会信息
    public NGuildMemberInfo memberInfo;

    public void SetGuildMemberInfo(NGuildMemberInfo item)
    {
        this.memberInfo = item;
        if (this.nickname != null) this.nickname.text = this.memberInfo.Info.Name;
        if (this.@class != null) this.@class.text = this.memberInfo.Info.Class.ToString();
        if (this.level != null) this.level.text = this.memberInfo.Info.Level.ToString();
        if (this.title != null) this.title.text = this.memberInfo.Title.ToString();
        if (this.joinTime != null) this.joinTime.text = TimeUtil.GetTime(this.memberInfo.joinTime).ToShortDateString();
        if (this.status != null) this.status.text = this.memberInfo.Status == 1 ? "在线" : TimeUtil.GetTime(this.memberInfo.joinTime).ToShortDateString();
    }
}
