using Managers;
using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGuildApplyList : UIWindow
{
    public GameObject itemPrefab;
    public ListView listMain;
    public Transform itemRoot;

    // Start is called before the first frame update
    void Start()
    {
        GuildService.Instance.OnGuildUpdate += UpdateList;
        GuildService.Instance.SendGuildListRequest();
        this.UpdateList();
    }
    void OnDestroy()
    {
        GuildService.Instance.OnGuildUpdate -= UpdateList;
    }

    private void UpdateList()
    {
        ClearList();
        InitItems();
    }

    private void ClearList()
    {
        this.listMain.RemoveAll();
    }

    private void InitItems()
    {
        foreach (var item in GuildManager.Instance.guildInfo.Applies)
        {
            GameObject go = Instantiate(itemPrefab, this.listMain.transform);
            UIGuildApplyItem uiItem = go.GetComponent<UIGuildApplyItem>();
            uiItem.SetApplyItemInfo(item);
            this.listMain.AddItem(uiItem);
        }
    }
}
