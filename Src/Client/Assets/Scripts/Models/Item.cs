using Common.Data;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Models
{
    public class Item
    {
        public int Id;
        public int Count;
        public ItemDefine Define;

        /// <summary>
        /// 可以通过网络消息来构建item
        /// </summary>
        /// <param name="item"></param>
        public Item(NItemInfo item) : this(item.Id, item.Count) { }

        /// <summary>
        /// 可以通过id和count来构建item
        /// 重载构造函数
        /// </summary>
        /// <param name="id"></param>
        /// <param name="count"></param>
        public Item(int id, int count)
        {
            this.Id = id;
            this.Count = count;
            this.Define = DataManager.Instance.Items[this.Id];
        }

        public override string ToString()
        {
            return string.Format("[Item] Id:{0}, Count:{1}", this.Id, this.Count);
        }
    }
}
