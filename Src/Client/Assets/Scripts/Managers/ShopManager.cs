using Common.Data;
using Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class ShopManager : Singleton<ShopManager>
    {
        public void Init()
        {
            NPCManager.Instance.RegisterNpcEvent(Common.Data.NpcDefine.NpcFunction.InvokeShop, OnOpenShop);     // 注册事件
        }

        #region 通过npc打开商店
        private bool OnOpenShop(NpcDefine npc)
        {
            // npc配置表中 Param 属性就是用于区分打开哪种商店
            this.ShowShop(npc.Param);   
            return true;
        }
        public void ShowShop(int shopId)
        {
            ShopDefine shop;
            if(DataManager.Instance.Shops.TryGetValue(shopId, out shop))
            {
                UIShop uiShop = UIManager.Instance.Show<UIShop>();
                if(uiShop != null)
                {
                    uiShop.SetShop(shop);
                }
            }
        }
        #endregion

        public bool BuyItem(int shopId, int shopItemId)
        {
            ItemService.Instance.SendBuyItem(shopId, shopItemId);
            return true;
        }
    }
}
