using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Services
{
    class StatusService : Singleton<StatusService>
    {
        public delegate bool StatusNotifyHandler(NStatus status);

        Dictionary<StatusType, StatusNotifyHandler> eventMap = new Dictionary<StatusType, StatusNotifyHandler>();

        public void Init() { }

        /// <summary>
        /// 注册状态
        /// </summary>
        /// <param name="function"></param>
        /// <param name="action"></param>
        public void RegisterStatusNofity(StatusType function, StatusNotifyHandler action)
        {
            if (!eventMap.ContainsKey(function))
            {
                eventMap[function] = action;
            }
            else
            {
                eventMap[function] += action;
            }
        }

        public StatusService()
        {
            // 监听状态协议的response —— StatusNotify
            MessageDistributer.Instance.Subscribe<StatusNotify>(this.OnStatusNotify);
        }
        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<StatusNotify>(this.OnStatusNotify);
        }

        /// <summary>
        /// 处理服务端发来的 状态协议response
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="notify"></param>
        private void OnStatusNotify(object sender, StatusNotify notify)
        {
            // 遍历
            foreach(NStatus status in notify.Status)
            {
                Notify(status);
            }
        }

        private void Notify(NStatus status)
        {
            Debug.LogFormat("[StatusService] StatusNotify:[{0}][{1}]{2}:{3}", status.Type, status.Action, status.Id, status.Value);

            // 如果是金币相关的处理
            if(status.Type == StatusType.Money)
            {
                if (status.Action == StatusAction.Add)
                    User.Instance.AddGold(status.Value);
                else if (status.Action == StatusAction.Delete)
                    User.Instance.AddGold(-status.Value);
            }

            // 不是金币，其他有系统注册，这儿就有发通知
            if (eventMap.TryGetValue(status.Type, out StatusNotifyHandler handler))
            {
                handler(status);
            }
        }
    }
}

