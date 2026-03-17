using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkillBridge.Message;
using Models;
using UnityEngine.Analytics;
using Services;

namespace Managers
{
    public class BagManager : Singleton<BagManager>
    {
        public int Unlocked;
        public BagItem[] Items;

        NBagInfo Info;

        unsafe public void Init(NBagInfo info)
        {
            this.Info = info;
            this.Unlocked = info.Unlocked;
            Items = new BagItem[this.Unlocked];
            if(info.Items != null && info.Items.Length >= this.Unlocked)
            {
                Analyze(info.Items);
            }
            else
            {
                Info.Items = new byte[sizeof(BagItem) * this.Unlocked];
                Reset();
            }

            // 注册道具变化通知
            StatusService.Instance.RegisterStatusNofity(StatusType.Item, OnItemStatusChanged);
        }

        private bool OnItemStatusChanged(NStatus status)
        {
            if (status.Action == StatusAction.Add)
            {
                this.AddItem(status.Id, status.Value);
                ItemManager.Instance.AddItem(status.Id, status.Value); // 同步 ItemManager
            }
            else if (status.Action == StatusAction.Delete)
            {
                this.RemoveItem(status.Id, status.Value);
            }

            // 通知 UIBag 刷新
            if (OnBagUpdate != null)
                OnBagUpdate();
            return true;
        }

        public System.Action OnBagUpdate;

        /// <summary>
        /// 背包整理
        /// </summary>
        public void Reset()
        {
            int i = 0;
            // 使用 ItemManager 的数据，BagManager 自己不维护数据
            foreach (var kv in ItemManager.Instance.Items)
            {
                // 加入的道具是否超出一个格子的Count上限
                if(kv.Value.Count <= kv.Value.Define.StackLimit)
                {
                    this.Items[i].ItemId = (ushort)kv.Key;
                    this.Items[i].Count = (ushort)kv.Value.Count;
                }
                else
                {
                    int count = kv.Value.Count;
                    while(count > kv.Value.Define.StackLimit)
                    {
                        this.Items[i].ItemId = (ushort)kv.Key;
                        this.Items[i].Count = (ushort)kv.Value.Define.StackLimit;   // 把当前的格子先填满
                        i++;    // 下一个格子
                        count -= kv.Value.Define.StackLimit;    // 下一个格子应该填的剩余的数量
                    }
                    this.Items[i].ItemId = (ushort)kv.Key;
                    this.Items[i].Count = (ushort)count;
                }
                i++;    // 下一个道具
            }
        }

        /// <summary>
        /// 把字节数组解析成结构体数组
        /// </summary>
        /// <param name="data"></param>
        unsafe void Analyze(byte[] data)
        {
            fixed(byte* pt = data)  // 指向data的指针
            {
                for(int i = 0; i < this.Unlocked; i++)
                {
                    // sizeof(BagItem)指的是一个格子占几个字节
                    BagItem* item = (BagItem*)(pt + i * sizeof(BagItem));   
                    Items[i] = *item;   // Items 是BagItem，结构体，属于值类型，赋值不会改变地址
                }
            }
        }
        /// <summary>
        /// 从结构体数组转变成字节数组
        /// </summary>
        /// <returns></returns>
        unsafe public NBagInfo GetBagInfo()
        {
            // 相当于从数组把值映射到内存里面
            fixed (byte* pt = Info.Items)
            {
                for (int i = 0; i < this.Unlocked; i++)
                {
                    BagItem* item = (BagItem*)(pt + i * sizeof(BagItem));
                    *item = Items[i];
                }
            }
            return this.Info;
        }

        /// <summary>
        /// 道具增加/减少时，背包同步增加/减少
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        public void AddItem(int itemId, int count)
        {
            ushort addCount = (ushort)count;
            for(int i = 0; i < Items.Length; i++)
            {
                if (this.Items[i].ItemId == itemId)
                {
                    // 不能超过一个格子能容纳该类物品的最大值，超过了需要移动至另一个格子
                    ushort canAdd = (ushort)(DataManager.Instance.Items[itemId].StackLimit - this.Items[i].Count);
                    if(canAdd >= addCount)
                    {
                        this.Items[i].Count += addCount;
                        addCount = 0;
                        break;
                    }
                    else
                    {
                        this.Items[i].Count += canAdd;
                        addCount -= canAdd;
                    }
                }
            }
            if(addCount > 0)
            {
                for(int i = 0; i < Items.Length; i++)
                {
                    if (this.Items[i].ItemId == 0)
                    {
                        this.Items[i].ItemId = (ushort)itemId;
                        this.Items[i].Count = addCount;
                        break;
                    }
                }
            }
        }

        public void RemoveItem(int itemId, int count)
        {

        }

    }
}
