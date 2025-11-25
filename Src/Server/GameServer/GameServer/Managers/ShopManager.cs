using Common;
using Common.Data;
using GameServer.Services;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class ShopManager : Singleton<ShopManager>
    {
        // 这个ShopManager不是为一个人服务的，而是为所有人，因此这儿传入的参数是sender，表示购买的用户
        public Result BuyItem(NetConnection<NetSession> sender, int shopId, int shopItemId)
        {
            // 验证商店ID和商店物品ID存不存在
            if (!DataManager.Instance.Shops.ContainsKey(shopId))
                return Result.Failed;

            ShopItemDefice shopItem;
            if (DataManager.Instance.ShopItems[shopId].TryGetValue(shopItemId, out shopItem))
            {
                Log.InfoFormat("[ShopManager] BuyItem: character:{0} :Item:{1} Count:{2} Price: {3}", sender.Session.Character.Id, shopItem.ItemID, shopItem.Count, shopItem.Price);

                // 钱够了才能购买
                if(sender.Session.Character.Gold >= shopItem.Price)
                {
                    sender.Session.Character.ItemManager.AddItem(shopItem.ItemID, shopItem.Count);
                    sender.Session.Character.Gold -= shopItem.Price;
                    DBService.Instance.Save();

                    return Result.Success;
                }
            }
            return Result.Failed;
        }
    }
}
