using Entities;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGuildItem : ListView.ListViewItem
{
    [Header("UI")]
    public Text guildIdText;
    public Text guildNameText;
    public Text membersText;
    public Text leaderText;

    [Header("Selection BG")]
    public Image background;
    public Sprite normalBg;
    public Sprite selectedBg;

    public override void onSelected(bool selected)
    {
        this.background.overrideSprite = selected ? selectedBg : normalBg;
    }

    public NGuildInfo Info { get; private set; }

    public void SetGuildInfo(NGuildInfo info)
    {
        this.Info = info;

        if (info == null)
        {
            if (guildIdText != null) guildIdText.text = "ID: -";
            if (guildNameText != null) guildNameText.text = "-";
            if (membersText != null) membersText.text = "-/-";
            if (leaderText != null) leaderText.text = "-";
            return;
        }

        if (guildIdText != null) guildIdText.text = $"ID:{info.Id}";
        if (guildNameText != null) guildNameText.text = info.GuildName;
        if (membersText != null) membersText.text = info.memberCount.ToString();
        if (leaderText != null) leaderText.text = info.leaderName;
    }
}
