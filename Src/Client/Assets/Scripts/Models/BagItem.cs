using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Models
{
    // 结构布局的属性，代表这个结构体在内存中的存储格式
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BagItem
    {
        // ushort是16位（2字节）
        public ushort ItemId;   // 每个格子的Id  
        public ushort Count;    // 该格子上的物品有几个

        public static BagItem zero = new BagItem { ItemId = 0, Count = 0 };

        public BagItem(int itemId, int count)
        {
            this.ItemId = (ushort)itemId;
            this.Count = (ushort)count;
        }

        public static bool operator == (BagItem lhs, BagItem rhs)
        {
            return lhs.ItemId == rhs.ItemId && lhs.Count == rhs.Count;
        }
        public static bool operator !=(BagItem lhs, BagItem rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// 判断两个物品是不是一样
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object other)
        {
            if(other is BagItem)
            {
                return Equals((BagItem)other);
            }
            return false;
        }
        public bool Equals(BagItem other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return ItemId.GetHashCode() ^ (Count.GetHashCode() << 2);
        }
    }

}
