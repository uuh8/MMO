using Common.Data;
using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Managers;

public class UIEquipItem : MonoBehaviour, IPointerClickHandler
{
    public Image icon;
    public Text title;
    public Text level;
    public Text limitClass;
    public Text limitCategory;

    public Image background;
    public Sprite normalBg;
    public Sprite selectedBg;

    private bool selected;
    public bool Selected
    {
        get { return selected; }
        set
        {
            selected = value;
            this.background.overrideSprite = selected ? selectedBg : normalBg;
        }
    }

    public int index { get; set; }
    private UICharEquip owner;
    private Item item;
    bool isEquiped = false; // 装备列表还是非装备列表

    /// <summary>
    /// UIShop使用，用于每个商品的初始化
    /// </summary>
    /// <param name="id"></param>
    /// <param name="shopItem"></param>
    /// <param name="owner"></param>
    public void SetEquipItem(int idx, Item item, UICharEquip owner, bool equiped)
    {
        this.owner = owner;
        this.index = idx;
        this.item = item;
        this.isEquiped = equiped;

        if (this.title != null) this.title.text = this.item.Define.Name;
        if (this.level != null) this.level.text = this.item.Define.Level.ToString();
        if (this.limitClass != null) this.limitClass.text = item.Define.LimitClass.ToString();
        if (this.limitCategory != null) this.limitCategory.text = item.Define.Category;
        if (this.icon != null) this.icon.overrideSprite = Resloader.Load<Sprite>(this.item.Define.Icon);
    }

    /// <summary>
    /// 实现的点击处理器接口IPointerClickHandler
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (this.isEquiped)
        {
            // 如果是已穿上的装备，点击就执行卸下操作
            UnEquip();
        }
        else
        {
            // 先判断有没有选中
            if (this.selected)
            {
                DoEquip();
                this.Selected = false;
            }else
                this.Selected = true;
        }
    }

    #region 穿上/卸下装备 （会调用UICharEquip）
    private void DoEquip()
    {
        var msg = MessageBox.Show(string.Format("要装备[{0}]吗？", this.item.Define.Name), "确认", MessageBoxType.Confirm);
        msg.OnYes = () =>
        {
            // 如果槽上已有其他装备，询问是否替换
            var oldEquip = EquipManager.Instance.GetEquip(item.EquipInfo.Slot);
            if(oldEquip != null)
            {
                var newmsg = MessageBox.Show(string.Format("要替换掉[{0}]吗？", oldEquip.Define.Name), "确认", MessageBoxType.Confirm);
                newmsg.OnYes = () =>
                {
                    this.owner.DoEquip(this.item);
                };
            }
            else
            {
                this.owner.DoEquip(this.item);
            }
        };
    }
    private void UnEquip()
    {
        var msg = MessageBox.Show(string.Format("要卸下装备[{0}]吗？", this.item.Define.Name), "确认", MessageBoxType.Confirm);
        msg.OnYes = () =>
        {
            this.owner.UnEquip(this.item);
        };
    }
    #endregion
}
