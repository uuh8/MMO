using Managers;
using Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Services
{
    class QuestService : Singleton<QuestService>, IDisposable
    {
        public QuestService()
        {
            MessageDistributer.Instance.Subscribe<QuestAcceptResponse>(this.OnQuestAccept);
            MessageDistributer.Instance.Subscribe<QuestSubmitResponse>(this.OnQuestSubmit);
        }
        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<QuestAcceptResponse>(this.OnQuestAccept);
            MessageDistributer.Instance.Unsubscribe<QuestSubmitResponse>(this.OnQuestSubmit);
        }

        #region 发送信息

        public bool SendQuestAccept(Quest quest)
        {
            Debug.Log("[QuestService] SendQuestAccept");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.questAccept = new QuestAcceptRequest();
            message.Request.questAccept.QuestId = quest.Define.ID;
            NetClient.Instance.SendMessage(message);
            return true;
        }
        public bool SendQuestSubmit(Quest quest)
        {
            Debug.Log("[QuestService] SendQuestSubmit");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.questSubmit = new QuestSubmitRequest();
            message.Request.questSubmit.QuestId = quest.Define.ID;
            NetClient.Instance.SendMessage(message);
            return true;
        }
        #endregion

        #region 响应服务器信息

        private void OnQuestAccept(object sender, QuestAcceptResponse message)
        {
            Debug.LogFormat("[QuestService] OnQuestAccept:{0}, ERR:{1}", message.Result, message.Errormsg);
            if (message.Result == Result.Success)
                QuestManager.Instance.OnQuestAccepted(message.Quest);
            else
                MessageBox.Show("[QuestService] 任务接收失败", "错误", MessageBoxType.Error);
        }
        private void OnQuestSubmit(object sender, QuestSubmitResponse message)
        {
            Debug.LogFormat("[QuestService] OnQuestSubmit:{0}, ERR:{1}", message.Result, message.Errormsg);
            if (message.Result == Result.Success)
                QuestManager.Instance.OnQuestSubmited(message.Quest);
            else
                MessageBox.Show("[QuestService] 任务接收失败", "错误", MessageBoxType.Error);
        }
        #endregion

    }

}
