using Models;
using Services;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Events;

namespace Managers
{
    public enum NpcQuestStatus
    {
        None = 0,   // 无任务
        Complete,   // 拥有已完成可提交任务
        Available,  // 拥有可接受任务
        InComplete  // 拥有未完成任务
    }

    class QuestManager : Singleton<QuestManager>
    {
        // 所有有效任务
        public List<NQuestInfo> questInfos;
        // 所有任务
        public Dictionary<int, Quest> allQuests = new Dictionary<int, Quest>();
        // 分npc的，分状态的任务（第一个int是npc的id）
        public Dictionary<int, Dictionary<NpcQuestStatus, List<Quest>>> npcQuests = new Dictionary<int, Dictionary<NpcQuestStatus, List<Quest>>>();
        
        
        public UnityAction<Quest> onQuestStatusChanged; // 让NpcController订阅

        public void Init(List<NQuestInfo> quests)
        {
            this.questInfos = quests;
            allQuests.Clear();
            this.npcQuests.Clear();
            InitQuests();
        }
        private void InitQuests()
        {
            // 初始化已接受的任务
            foreach (var info in this.questInfos)
            {
                Quest quest = new Quest(info);
                this.allQuests[quest.Info.QuestId] = quest;
            }

            // 初始化可以接受的任务
            this.CheckAvailableQuests();

            // 把所有任务遍历一遍，加到npc身上
            foreach(var kv in this.allQuests)
            {
                this.AddNpcQuest(kv.Value.Define.AcceptNPC, kv.Value);
                this.AddNpcQuest(kv.Value.Define.SubmitNPC, kv.Value);
            }
        }

        /// <summary>
        /// 初始化可接任务
        /// </summary>
        private void CheckAvailableQuests()
        {
            // 初始化可用任务
            foreach (var kv in DataManager.Instance.Quests)
            {
                // 任务不符合职业
                if (kv.Value.LimitClass != CharacterClass.None && kv.Value.LimitClass != User.Instance.CurrentCharacter.Class)
                    continue;
                // 任务不符合等级
                if (kv.Value.LimitLevel > User.Instance.CurrentCharacter.Level)
                    continue;
                // 任务已经存在
                if (this.allQuests.ContainsKey(kv.Key))
                    continue;

                if (kv.Value.PreQuest > 0)
                {
                    Quest preQuest;
                    if (this.allQuests.TryGetValue(kv.Value.PreQuest, out preQuest))    // 获取前置任务
                    {
                        if (preQuest.Info == null)
                            continue;   // 前置任务未接取
                        if (preQuest.Info.Status != QuestStatus.Finished)
                            continue;   // 前置任务未完成
                    }
                    else
                        continue;       //  前置任务还没接
                }
                Quest quest = new Quest(kv.Value);
                this.allQuests[quest.Define.ID] = quest;
            }
        }
        private void AddNpcQuest(int npcId, Quest quest)
        {
            if (!this.npcQuests.ContainsKey(npcId))
                this.npcQuests[npcId] = new Dictionary<NpcQuestStatus, List<Quest>>();

            // 每个npc身上有三个列表
            List<Quest> availables;     // 可接受的任务
            List<Quest> completes;      // 已完成的任务
            List<Quest> incomplates;    // 未完成的任务

            // 初始化三个列表
            if (!this.npcQuests[npcId].TryGetValue(NpcQuestStatus.Available, out availables))
            {
                availables = new List<Quest>();
                this.npcQuests[npcId][NpcQuestStatus.Available] = availables;
            }
            if (!this.npcQuests[npcId].TryGetValue(NpcQuestStatus.Complete, out completes))
            {
                completes = new List<Quest>();
                this.npcQuests[npcId][NpcQuestStatus.Complete] = completes;
            }
            if (!this.npcQuests[npcId].TryGetValue(NpcQuestStatus.InComplete, out incomplates))
            {
                incomplates = new List<Quest>();
                this.npcQuests[npcId][NpcQuestStatus.InComplete] = incomplates;
            }
            
            // 根据是 接任务的npc 还是 交任务的npc，对这三个列表进行操作
            if (quest.Info == null)
            {
                if (npcId == quest.Define.AcceptNPC && !this.npcQuests[npcId][NpcQuestStatus.Available].Contains(quest))
                {
                    this.npcQuests[npcId][NpcQuestStatus.Available].Add(quest);
                }
            }
            else
            {
                if (npcId == quest.Define.SubmitNPC && quest.Info.Status == QuestStatus.Completed)
                {
                    if (!this.npcQuests[npcId][NpcQuestStatus.Complete].Contains(quest))
                    {
                        this.npcQuests[npcId][NpcQuestStatus.Complete].Add(quest);
                    }
                }
                if (npcId == quest.Define.SubmitNPC && quest.Info.Status == QuestStatus.InProgress)
                {
                    if (!this.npcQuests[npcId][NpcQuestStatus.InComplete].Contains(quest))
                    {
                        this.npcQuests[npcId][NpcQuestStatus.InComplete].Add(quest);
                    }
                }
            }
        }


        #region 给 npc系统 的开放接口
        /// <summary>
        /// 查询NPC任务状态（是否有任务）
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        public NpcQuestStatus GetQuestStatusByNpc(int npcId)
        {
            Dictionary<NpcQuestStatus, List<Quest>> status = new Dictionary<NpcQuestStatus, List<Quest>>();
            if(this.npcQuests.TryGetValue(npcId, out status))
            {
                if (status[NpcQuestStatus.Complete].Count > 0)
                    return NpcQuestStatus.Complete;
                if (status[NpcQuestStatus.Available].Count > 0)
                    return NpcQuestStatus.Available;
                if (status[NpcQuestStatus.InComplete].Count > 0)
                    return NpcQuestStatus.InComplete;
            }
            return NpcQuestStatus.None;
        }
        /// <summary>
        /// 打开 NPC 的 对话框（UIQuestDialog）(由NPCManager调用）
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        public bool OpenNpcQuest(int npcId)
        {
            Dictionary<NpcQuestStatus, List<Quest>> status = new Dictionary<NpcQuestStatus, List<Quest>>();
            if (this.npcQuests.TryGetValue(npcId, out status))
            {
                if (status[NpcQuestStatus.Complete].Count > 0)
                    return ShowQuestDialog(status[NpcQuestStatus.Complete].First());
                if (status[NpcQuestStatus.Available].Count > 0)
                    return ShowQuestDialog(status[NpcQuestStatus.Available].First());
                if (status[NpcQuestStatus.InComplete].Count > 0)
                    return ShowQuestDialog(status[NpcQuestStatus.InComplete].First());
            }
            return false;
        }
        
        #endregion

        /// <summary>
        /// 显示任务对话框（点击NPC的时候出现）
        /// </summary>
        /// <param name="quest"></param>
        /// <returns></returns>
        private bool ShowQuestDialog(Quest quest)
        {
            if(quest.Info == null || quest.Info.Status == QuestStatus.Completed)
            {
                // 只有 接任务 或 交任务 才弹 UIQuestDialog
                UIQuestDialog dlg = UIManager.Instance.Show<UIQuestDialog>();
                dlg.SetQuest(quest);
                dlg.OnClose += OnQuestDialogClose;  // 处理关闭事件
                return true;
            }
            if(quest.Info != null || quest.Info.Status == QuestStatus.Completed)
            {
                if (!string.IsNullOrEmpty(quest.Define.DialogIncomplete))
                    MessageBox.Show(quest.Define.DialogIncomplete);
            }
            return false;
        }
        /// <summary>
        /// 处理关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="result"></param>
        private void OnQuestDialogClose(UIWindow sender, UIWindow.WindowResult result)
        {
            UIQuestDialog dlg = (UIQuestDialog)sender;
            if(result == UIWindow.WindowResult.Yes)
            {
                if (dlg.quest.Info == null) // 接受任务
                    QuestService.Instance.SendQuestAccept(dlg.quest);
                else // 完成任务
                    QuestService.Instance.SendQuestSubmit(dlg.quest);
            }
            else if(result == UIWindow.WindowResult.No)
            {
                MessageBox.Show(dlg.quest.Define.DialogDeny);
            }
        }

        private Quest RefreshQuestStatus(NQuestInfo quest)
        { 
            // 要使用服务器的信息来更新npc身上的任务
            // 先清理掉npc身上的任务
            this.npcQuests.Clear();
            Quest result;
            // 把服务端发来的任务信息同步到本地的任务列表
            if (this.allQuests.ContainsKey(quest.QuestId))
            {
                // 如果allQuests中存在，更新一下
                this.allQuests[quest.QuestId].Info = quest;
                result = this.allQuests[quest.QuestId];
            }
            else
            {
                // 如果是新接的任务，new一个，再加入任务列表
                result = new Quest(quest);
                this.allQuests[quest.QuestId] = result;
            }

            CheckAvailableQuests();

            foreach(var kv in this.allQuests)
            {
                this.AddNpcQuest(kv.Value.Define.AcceptNPC, kv.Value);
                this.AddNpcQuest(kv.Value.Define.SubmitNPC, kv.Value);
            }

            // 任务状态通知（NpcController请求的）
            if (onQuestStatusChanged != null)
                onQuestStatusChanged(result);
            return result;
        }
        
        public void OnQuestAccepted(NQuestInfo info)
        {
            var quest = this.RefreshQuestStatus(info);
            MessageBox.Show(quest.Define.DialogAccept);
        }
        public void OnQuestSubmited(NQuestInfo info)
        {
            var quest = this.RefreshQuestStatus(info);
            MessageBox.Show(quest.Define.DialogFinish);
        }

    }
}