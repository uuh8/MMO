using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkillBridge.Message;
using Unity.Collections;
using Models;
using Managers;

namespace Services
{
    class ItemService : Singleton<ItemService>, IDisposable
    {
        public ItemService()
        {
            MessageDistributer.Instance.Subscribe<ItemBuyResponse>(this.OnItemBuy);
            MessageDistributer.Instance.Subscribe<ItemEquipResponse>(this.OnItemEquip);
        }
        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<ItemBuyResponse>(this.OnItemBuy);
            MessageDistributer.Instance.Unsubscribe<ItemEquipResponse>(this.OnItemEquip);
        }

        #region 消息发送

        /// <summary>
        /// 发送购买的消息
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="shopItemId"></param>
        public void SendBuyItem(int shopId, int shopItemId)
        {
            Debug.Log("SendBuyItem");

            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.itemBuy = new ItemBuyRequest();
            message.Request.itemBuy.shopId = shopId;
            message.Request.itemBuy.shopItemId = shopItemId;
            NetClient.Instance.SendMessage(message);
        }

        public bool SendEquipItem(Item equip, bool isEquip)
        {
            if (pendingEquip != null)
                return false;
            Debug.Log("SendEquipItem");

            pendingEquip = equip;
            this.isEquip = isEquip;

            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.itemEquip = new ItemEquipRequest();
            message.Request.itemEquip.itemId = equip.Id;
            message.Request.itemEquip.isEquip = isEquip;
            NetClient.Instance.SendMessage(message);
            return true;
        }
        #endregion

        #region 服务端消息处理

        /// <summary>
        /// 用于处理接收
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void OnItemBuy(object sender, ItemBuyResponse response)
        {
            MessageBox.Show("购买结果：" + response.Result + "\n" + response.Errormsg, "购买完成");
        }


        /// <summary>
        /// 穿戴装备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        Item pendingEquip = null;   // 发送的时候就有这个变量，后续就知道穿戴或卸下的是什么装备
        bool isEquip;
        private void OnItemEquip(object sender, ItemEquipResponse response)
        {
            if(response.Result == Result.Success)
            {
                if(pendingEquip != null)
                {
                    if (this.isEquip)
                        EquipManager.Instance.OnEquipItem(pendingEquip);
                    else
                        EquipManager.Instance.OnUnEquipItem(pendingEquip.EquipInfo.Slot);
                    pendingEquip = null;
                }
            }
        }
        #endregion

    }
}
