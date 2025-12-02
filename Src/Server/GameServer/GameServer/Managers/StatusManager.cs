using GameServer.Entities;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    /* 状态管理器和道具管理器一样都是 “绑定角色” */
    class StatusManager
    {
        Character Owner;

        private List<NStatus> Status { get; set; }

        public bool HasStatus
        {
            get { return this.Status.Count > 0; }
        }

        public StatusManager(Character owner)
        {
            this.Owner = owner;
            this.Status = new List<NStatus>();
        }

        public void AddStatus(StatusType type, int id, int value, StatusAction action)
        {
            this.Status.Add(new NStatus()
            {
                Type = type,
                Id = id,
                Value = value,
                Action = action
            });
        }

        /// <summary>
        /// 添加 加金币 的状态变化
        /// </summary>
        /// <param name="goldDelta"></param>
        public void AddGoldChange(int goldDelta)
        {
            if (goldDelta > 0)
            {
                this.AddStatus(StatusType.Money, 0, goldDelta, StatusAction.Add);
            }
            if (goldDelta < 0)
            {
                this.AddStatus(StatusType.Money, 0, -goldDelta, StatusAction.Delete);
            }
        }
        /// <summary>
        /// 添加 物品状态变化 的状态变化
        /// </summary>
        /// <param name="id"></param>
        /// <param name="count"></param>
        /// <param name="action"></param>
        public void AddItemChange(int id, int count, StatusAction action)
        {
            this.AddStatus(StatusType.Item, id, count, action);
        }

        /// <summary>
        /// 把当前状态的所有变化都放到“状态通知”的这个message里面
        /// </summary>
        /// <param name="message"></param>
        public void ApplyResponse(NetMessageResponse message)
        {
            if (message.statusNotify == null)
                message.statusNotify = new StatusNotify();
            foreach(var status in this.Status)
            {
                message.statusNotify.Status.Add(status);
            }
            this.Status.Clear();
        }
    }
}
