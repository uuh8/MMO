using Models;
using Services;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class EquipManager : Singleton<EquipManager>
    {
        public delegate void OnEquipChangeHandler();
        public event OnEquipChangeHandler OnEquipChanged;

        public Item[] Equips = new Item[(int)EquipSlot.SlotMax];

        byte[] Data;    // 用于维护字节和int数组的转换，因为装备在服务端是以字节形式保存的，每个槽4个字节

        /// <summary>
        /// 和背包同理，在UserService中初始化
        /// </summary>
        /// <param name="data"></param>
        unsafe public void Init(byte[] data)
        {
            this.Data = data;
            this.ParseEquipData(data);
        }

        #region 开放方法
        /// <summary>
        /// 开放一个方法用于检测角色是否穿了某个装备
        /// </summary>
        /// <param name="equipId"></param>
        /// <returns></returns>
        public bool Contains(int equipId)
        {
            for (int i = 0; i < this.Equips.Length; i++)
            {
                if (Equips[i] != null && this.Equips[i].Id == equipId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 开放一个方法方便其他系统检测某个slot放了什么装备
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public Item GetEquip(EquipSlot slot)
        {
            return Equips[(int)slot];
        }
        #endregion

        #region int/byte 的转换处理
        /// <summary>
        /// 从字节到整型变换（收到服务端发来的装备字节信息后用于处理这些字节）
        /// </summary>
        /// <param name="data"></param>
        unsafe void ParseEquipData(byte[] data)
        {
            fixed (byte* pt = this.Data)
            {
                for (int i = 0; i < this.Equips.Length; i++)
                {
                    int itemId = *(int*)(pt + i * sizeof(int));
                    if (itemId > 0)
                        Equips[i] = ItemManager.Instance.Items[itemId];
                    else
                        Equips[i] = null;
                }
            }
        }
        /// <summary>
        /// 从整型到字节变换（需要发送给服务器的时候使用，因为服务器是使用字节保存装备的）
        /// </summary>
        /// <returns></returns>
        unsafe public byte[] GetEquipData()
        {
            fixed (byte* pt = this.Data)
            {
                for (int i = 0; i < (int)EquipSlot.SlotMax; i++)
                {
                    int* itemid = (int*)(pt + i * sizeof(int));
                    if (Equips[i] == null)
                        *itemid = 0;
                    else
                        *itemid = Equips[i].Id;
                }
            }
            return this.Data;
        }
        #endregion

        #region 网络消息响应/发送
        /// <summary>
        /// 穿上/卸下装备 （发请求）
        /// </summary>
        /// <param name="equip"></param>
        public void EquipItem(Item equip)
        {
            ItemService.Instance.SendEquipItem(equip, true);
        }
        public void UnEquipItem(Item equip)
        {
            ItemService.Instance.SendEquipItem(equip, false);
        }

        /// <summary>
        /// 收到 穿上/卸下装备 （处理请求）
        /// </summary>
        /// <param name="equip"></param>
        public void OnEquipItem(Item equip)
        {
            // 检查slot是否为空或者已经穿上
            if (this.Equips[(int)equip.EquipInfo.Slot] != null && this.Equips[(int)equip.EquipInfo.Slot].Id == equip.Id)
            {
                return;
            }
            // 从道具系统取出装备放到slot上
            this.Equips[(int)equip.EquipInfo.Slot] = ItemManager.Instance.Items[equip.Id];

            if (OnEquipChanged != null)
                OnEquipChanged();
        }
        public void OnUnEquipItem(EquipSlot slot)
        {
            if (this.Equips[(int)slot] != null)
            {
                this.Equips[(int)slot] = null;

                if (OnEquipChanged != null)
                    OnEquipChanged();
            }
        }
        #endregion

    }
}
