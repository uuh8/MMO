using JetBrains.Annotations;
using Models;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Managers;

public class UICharEquip : UIWindow
{
    public Text title;
    public Text money;

    public GameObject itemPrefab;
    public GameObject itemEquipedPrefab;

    public Transform itemListRoot;

    public List<Transform> slots;

    // Start is called before the first frame update
    void Start()
    {
        RefreshUI();
        EquipManager.Instance.OnEquipChanged += RefreshUI;  // 只要装备变化了（装备/卸下），就刷新一次UI
    }

    void OnDestroy()
    {
        EquipManager.Instance.OnEquipChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        ClearAllEquipList();    // 左边装备列表清空
        InitAllEquipItems();    // 初始化装备
        ClearEquipedList();     // 把右边已经装备的列表清空
        InitEquipedItems();     // 重新初始化一遍
        this.money.text = User.Instance.CurrentCharacter.Gold.ToString();   // 金钱刷新
    }

    /// <summary>
    /// 初始化所有装备列表
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void InitAllEquipItems()
    {
        foreach(var kv in ItemManager.Instance.Items)
        {
            // 只显示装备
            if(kv.Value.Define.Type == SkillBridge.Message.ItemType.Equip)
            {
                // 已经装备就不显示了
                if (EquipManager.Instance.Contains(kv.Key) && kv.Value.Define.LimitClass == User.Instance.CurrentCharacter.Class)
                    continue;
                GameObject go = Instantiate(itemPrefab, itemListRoot, false);
                UIEquipItem ui = go.GetComponent<UIEquipItem>();
                ui.SetEquipItem(kv.Key, kv.Value, this, false);
            }
        }
    }
    /// <summary>
    /// 初始化已装备的列表(右边列表)
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void InitEquipedItems()
    {
        // 检查每个槽上有没有装备，如果有装备，就生成一个，设置装备信息
        for(int i = 0; i < (int)EquipSlot.SlotMax; i++)
        {
            var item = EquipManager.Instance.Equips[i];
            if(item != null)
            {
                GameObject go = Instantiate(itemEquipedPrefab, slots[i], false);
                UIEquipItem ui = go.GetComponent<UIEquipItem>();
                ui.SetEquipItem(i, item, this, true);
            }
        }
    }

    /// <summary>
    /// 清空左边装备列表
    /// </summary>
    private void ClearAllEquipList()
    {
        foreach(var item in itemListRoot.GetComponentsInChildren<UIEquipItem>())
        {
            Destroy(item.gameObject);
        }
    }
    /// <summary>
    /// 清空右边装备列表
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void ClearEquipedList()
    {
        foreach(var item in slots)
        {
            if (item.childCount > 0)
                Destroy(item.GetChild(0).gameObject);
        }
    }

    public void DoEquip(Item item)
    {
        EquipManager.Instance.EquipItem(item);
    }
    public void UnEquip(Item item)
    {
        EquipManager.Instance.UnEquipItem(item);
    }
}
