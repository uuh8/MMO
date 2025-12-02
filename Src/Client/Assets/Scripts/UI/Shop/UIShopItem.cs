using Common.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIShopItem : MonoBehaviour, ISelectHandler
{
    public Image icon;
    public Text title;
    public Text price;
    public Text limitClass; // 道具的职业限制
    public Text count;
    public Image background;

    public Sprite normalBg;
    public Sprite selectedBg;

    // 验证是否被选中，区分选中和没选中的状态
    private bool selected;
    public bool Selected
    {
        get { return selected;}
        set
        {
            selected = value;
            this.background.overrideSprite = selected ? selectedBg : normalBg;
        }
    }
    public int ShopItemID { get; set; }
    private UIShop shop;

    private ItemDefine itemDefine;
    private ShopItemDefice ShopItem { get; set; }

    /// <summary>
    /// UIShop使用，用于每个商品的初始化
    /// </summary>
    /// <param name="id"></param>
    /// <param name="shopItem"></param>
    /// <param name="owner"></param>
    public void SetShopItem(int id, ShopItemDefice shopItem, UIShop owner)
    {
        this.shop = owner;
        this.ShopItemID = id;
        this.ShopItem = shopItem;
        this.itemDefine = DataManager.Instance.Items[this.ShopItem.ItemID];

        this.title.text = this.itemDefine.Name;
        this.count.text = "x" + shopItem.Count.ToString();
        this.price.text = shopItem.Price.ToString();
        this.limitClass.text = this.itemDefine.LimitClass.ToString();
        this.icon.overrideSprite = Resloader.Load<Sprite>(itemDefine.Icon);
    }

    /// <summary>
    /// 重写的接口
    /// </summary>
    /// <param name="eventData"></param>
    public void OnSelect(BaseEventData eventData)
    {
        this.Selected = true;
        this.shop.SelectShopItem(this);
    }
}
