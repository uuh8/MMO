using Common.Data;
using Managers;
using Models;
using Services;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : Singleton<ItemManager>
{
    public Dictionary<int, Item> Items = new Dictionary<int, Item>();
    internal void Init(List<NItemInfo> items)
    {
        this.Items.Clear();
        // 从网络填充角色道具的数据
        foreach(var info in items)
        {
            Item item = new Item(info);
            this.Items.Add(item.Id, item);

            Debug.LogFormat("[ItemManager] ItemManager:Init[{0}]", item);
        }
        // 注册通知
        StatusService.Instance.RegisterStatusNofity(StatusType.Item, OnItemNotify);
    }

    private bool OnItemNotify(NStatus status)
    {
        if(status.Action == StatusAction.Add)
        {
            this.AddItem(status.Id, status.Value);
        }
        if(status.Action == StatusAction.Delete)
        {
            this.RemoveItem(status.Id, status.Value);
        }
        return true;
    }


    /// <summary>
    /// 添加/删除物品
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="count"></param>
    public void AddItem(int itemId, int count)
    {
        Item item = null;
        if(this.Items.TryGetValue(itemId, out item))
        {
            // 道具已存在，直接添加数量
            item.Count += count;
        }
        else
        {
            // 道具不存在，先构造
            item = new Item(itemId, count);
            this.Items.Add(itemId, item);
        }
        // 道具系统更新了，背包系统也要更新
        BagManager.Instance.AddItem(itemId, count);
    }
    private void RemoveItem(int itemId, int count)
    {
        if (!this.Items.ContainsKey(itemId))
            return;

        Item item = this.Items[itemId];
        if (item.Count < count)
            return;
        item.Count -= count;

        BagManager.Instance.RemoveItem(itemId, count);
    }

    public ItemDefine GetItem(int itemId)
    {
        return null;
    }
    public bool UseItem(int itemId)
    {
        return false;
    }
    public bool UseItem(ItemDefine item)
    {
        return false;
    }
}
