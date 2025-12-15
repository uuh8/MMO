using Common.Data;
using Managers;
using Models;
using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFriends : UIWindow
{
    public GameObject friendItemPrefab;
    public ListView listMain;
    public Transform itemRoot;      // 在哪个根节点创建列表
    public UIFriendItem selectedItem;

    void Start()
    {
        FriendService.Instance.OnFriendUpdate = RefreshUI;  // 注册事件，网络有新消息的时候刷新UI
        this.listMain.onItemSelected += this.OnFriendSelected;
        RefreshUI();    // 启动时自动刷新一次UI
    }

    public void OnFriendSelected(ListView.ListViewItem item)
    {
        this.selectedItem = item as UIFriendItem;
    }


    public void OnClickFriendAdd()
    {
        InputBox.Show("输入要添加的好友名称或ID", "添加好友").OnSubmit += OnFriendAddSubmit;
    }

    private bool OnFriendAddSubmit(string input, out string tips)
    {
        tips = "";
        int friendId = 0;
        string friendName = "";
        if (!int.TryParse(input, out friendId))
            friendName = input;
        if(friendId == User.Instance.CurrentCharacter.Id || friendName == User.Instance.CurrentCharacter.Name)
        {
            tips = "不能添加自己为好友";
            return false;
        }

        FriendService.Instance.SendFriendAddRequest(friendId, friendName);
        return true;
    }

    public void OnClickFriendChar()
    {
        MessageBox.Show("暂未开放");
    }
    public void OnClickFriendRemove()
    {
        if(selectedItem == null)
        {
            MessageBox.Show("请选择要删除的好友");
            return;
        }
        MessageBox.Show(string.Format("确定要删除好友[{0}]吗？", selectedItem.Info.friendInfo.Name), "删除好友", MessageBoxType.Confirm, "删除", "取消").OnYes = () =>
        {
            FriendService.Instance.SendFriendRemoveRequest(this.selectedItem.Info.Id, this.selectedItem.Info.friendInfo.Id);
        };
    }

    private void RefreshUI()
    {
        ClearFriendList();    // 清空主线/支线两个列表
        InitFriendItems();    // 重建两个列表
    }

    private void InitFriendItems()
    {
        foreach (var Item in FriendManager.Instance.allFriends)
        {
            GameObject go = Instantiate(friendItemPrefab, this.listMain.transform);
            UIFriendItem uiItem = go.GetComponent<UIFriendItem>();
            uiItem.SetFriendInfo(Item);
            this.listMain.AddItem(uiItem);
        }
    }
    private void ClearFriendList()
    {
        this.listMain.RemoveAll();
    }
}
