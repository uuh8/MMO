using Common.Data;
using Managers;
using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIShop : UIWindow
{
    public Text title;
    public Text money;
    public GameObject shopItem;

    ShopDefine shop;
    public Transform[] itemRoot;

    void Start()
    {
        StartCoroutine(InitItems());     
    }
    IEnumerator InitItems()
    {
        int count = 0;
        int page = 0;
        foreach (var kv in DataManager.Instance.ShopItems[shop.ID])
        {
            if(kv.Value.Status > 0)
            {
                GameObject go = Instantiate(shopItem, itemRoot[page]);
                UIShopItem ui = go.GetComponent<UIShopItem>();
                ui.SetShopItem(kv.Key, kv.Value, this);
                count++;
                // 每页显示10个道具，超过就分页
                if(count >= 10)
                {
                    count = 0;
                    page++;
                    itemRoot[page].gameObject.SetActive(true);
                }
            }
        }
        yield return null;
    }

    /// <summary>
    /// NPC是商店的打开入口
    /// </summary>
    /// <param name="shop"></param>
    public void SetShop(ShopDefine shop)
    {
        this.shop = shop;
        this.title.text = shop.Name;
        this.money.text = User.Instance.CurrentCharacter.Gold.ToString();
    }

    /// <summary>
    /// 当前选中了哪个ShopItem
    /// </summary>
    private UIShopItem selectedItem;
    public void SelectShopItem(UIShopItem item)
    {
        if (selectedItem != null)
            selectedItem.Selected = false;
        selectedItem = item;
    }

    /// <summary>
    /// 点击购买按钮
    /// </summary>
    public void OnClickBuy()
    {
        if(this.selectedItem == null)
        {
            MessageBox.Show("请选择要购买的道具", "购买提示");
            return;
        }
        ShopManager.Instance.BuyItem(this.shop.ID, this.selectedItem.ShopItemID);
    }
}
