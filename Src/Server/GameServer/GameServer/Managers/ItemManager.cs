using Common;
using GameServer.Entities;
using GameServer.Models;
using GameServer.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    /* 该类是和Character绑定的，因此在Character实体类中调用 */
    class ItemManager
    {
        Character Owner;
        // 一个自动用于维护角色身上的所有道具
        public Dictionary<int, Item> Items = new Dictionary<int, Item>();

        // 构造函数
        public ItemManager(Character owner)
        {
            this.Owner = owner;

            foreach(var item in owner.Data.Items)
            {
                this.Items.Add(item.ItemID, new Item(item));
            }
        }

        /// <summary>
        /// 使用道具
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool UseItem(int itemId, int count = 1)
        {
            Log.InfoFormat("[ItemManager] [{0}]UseItem[{1}:{2}]", this.Owner.Data.ID, itemId, count);
            Item item = null;
            if(this.Items.TryGetValue(itemId, out item))
            {
                // 判断道具存在
                if(item.Count < count)
                    return false;

                // TODO 增加使用逻辑

                item.Remove(count);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 判断道具是否存在
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool HasItem(int itemId)
        {
            Item item = null;
            if(this.Items.TryGetValue(itemId, out item))
            {
                return item.Count > 0;
            }
            return false;
        }
        /// <summary>
        /// 获取道具
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public Item GetItem(int itemId)
        {
            Item item = null;
            this.Items.TryGetValue(itemId, out item);
            Log.InfoFormat("[ItemManager] [{0}]GetItem[{1}:{2}]", this.Owner.Data.ID, itemId, item);
            return item;
        }

        /// <summary>
        /// 增加道具
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool AddItem(int itemId, int count)
        {
            Item item = null;

            if(this.Items.TryGetValue(itemId, out item))
            {
                // 如果道具存在，直接添加
                item.Add(count);
            }
            else
            {
                // 如果道具不存在，在数据表中插入一条新数据
                TCharacterItem dbItem = new TCharacterItem();
                dbItem.CharacterID = Owner.Data.ID;
                dbItem.Owner = Owner.Data;
                dbItem.ItemID = itemId;
                dbItem.ItemCount = count;
                Owner.Data.Items.Add(dbItem);
                item = new Item(dbItem);
                this.Items.Add(itemId, item);
            }
            Log.InfoFormat("[ItemManager] [{0}]AddItem[{1}] addCount:{2}", this.Owner.Data.ID, item, count);
            return true;
        }
        /// <summary>
        /// 移除道具
        /// </summary>
        /// <param name="ItemId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool RemoveItem(int ItemId, int count)
        {
            // 判断道具是否存在
            if (!this.Items.ContainsKey(ItemId))
            {
                return false;
            }

            // 判断数量是否正确
            Item item = this.Items[ItemId];
            if (item.Count < count)
                return false;

            item.Remove(count);
            //道具/金币只要发生了变化，只做一件事，把变化量记录到 状态管理器StatusManager，状态管理器会自己处理后续
            this.Owner.StatusManager.AddItemChange(ItemId, count, StatusAction.Delete);
            Log.InfoFormat("[ItemManager] [{0}]RemoveItem[{1}] removeCount:{2}", this.Owner.Data.ID, item, count);
            //DBService.Instance.Save(); 
            return true;
        }

        /// <summary>
        /// 获取道具信息
        /// </summary>
        /// <param name="list"></param>
        public void GetItemInfos(List<NItemInfo> list)
        {
            foreach(var item in this.Items)
            {
                list.Add(new NItemInfo()
                {
                    Id = item.Value.ItemID,
                    Count = item.Value.Count
                });
            }
        }
    }
}
