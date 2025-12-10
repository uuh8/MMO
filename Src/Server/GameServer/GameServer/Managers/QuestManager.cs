using Common.Data;
using GameServer.Entities;
using GameServer.Services;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class QuestManager
    {

        #region 供Character初始化使用

        Character Owner;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner"></param>
        public QuestManager(Character owner)
        {
            this.Owner = owner;
        }

        /// <summary>
        /// 遍历数据库中角色身上的任务列表，填充到列表，方便后续发送给客户端
        /// </summary>
        /// <param name="list"></param>
        public void GetQuestInfos(List<NQuestInfo> list)
        {
            foreach (var quest in this.Owner.Data.Quests)
            {
                list.Add(GetQuestInfo(quest));
            }
        }

        /// <summary>
        /// 类型转换，从 TCharacterQuest 转换成 NQuestInfo，后续打包成 protobuf 发回客户端
        /// </summary>
        /// <param name="quest"></param>
        /// <returns></returns>
        public NQuestInfo GetQuestInfo(TCharacterQuest quest)
        {
            return new NQuestInfo()
            {
                QuestId = quest.QuestID,
                QuestGuid = quest.Id,
                Status = (QuestStatus)quest.Status,
                Targets = new int[3]
                {
                    (int)quest.Target1,
                    (int)quest.Target2,
                    (int)quest.Target3,
                }
            };
        }
        
        #endregion

        /// <summary>
        /// 接受任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="questId"></param>
        /// <returns></returns>
        public Result AcceptQuest(NetConnection<NetSession> sender, int questId)
        {
            // 后取当前接任务的角色是谁
            Character character = sender.Session.Character;

            // 校验配置表中有没有这个任务（是不是客户端假冒的，校验客户端信息）
            QuestDefine quest;
            if(DataManager.Instance.Quests.TryGetValue(questId, out quest))
            {
                /* Create和new的区别是Create() 是 Entity Framework 特有的方法，它不仅创建对象，还会将其与数据库上下文关联；new 是 C# 的基本操作符，只负责创建对象实例，不涉及任何数据库操作。
                 * 如果只是想在内存中创建一个对象而不涉及数据库操作，使用 new 就足够了。但如果打算将这个对象保存到数据库，使用 Create() 会更合适。
                 */
                TCharacterQuest dbquest = DBService.Instance.Entities.TCharacterQuests.Create();
                dbquest.QuestID = quest.ID;
                if(quest.Target1 == QuestTarget.None)
                {
                    // 该任务没有Target，直接完成
                    dbquest.Status = (int)QuestStatus.Completed;
                }
                else
                {
                    // 任务有Target，设置任务状态为 进行中
                    dbquest.Status = (int)QuestStatus.InProgress;
                }
                // 把db中的数据转换为网络数据，存入网络消息中
                sender.Session.Response.questAccept.Quest = this.GetQuestInfo(dbquest); // GetQuestInfo 用于将 TCharacterQuest 转换成 NQuestInfo
                character.Data.Quests.Add(dbquest);
                DBService.Instance.Save();
                return Result.Success;
            }
            else
            {
                // 客户端信息不实
                sender.Session.Response.questAccept.Errormsg = "任务不存在";
                return Result.Failed;
            }
        }
        /// <summary>
        /// 交任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="questId"></param>
        /// <returns></returns>
        public Result SubmitQuest(NetConnection<NetSession> sender, int questId)
        {
            // 后取当前接任务的角色是谁
            Character character = sender.Session.Character;

            QuestDefine quest;
            if (DataManager.Instance.Quests.TryGetValue(questId, out quest))
            {
                // 查询数据库
                TCharacterQuest dbquest = character.Data.Quests.Where(q => q.QuestID == questId).FirstOrDefault();
                if (dbquest != null)
                {
                    if(dbquest.Status != (int)QuestStatus.Completed)
                    {
                        // 还不是完成状态
                        sender.Session.Response.questSubmit.Errormsg = "任务未完成";
                        return Result.Failed;
                    }
                    dbquest.Status = (int)QuestStatus.Finished;
                    sender.Session.Response.questSubmit.Quest = this.GetQuestInfo(dbquest);
                    DBService.Instance.Save();

                    // 处理任务奖励
                    if (quest.RewardGold > 0)
                    {
                        character.Gold += quest.RewardGold;
                    }
                    if (quest.RewardExp > 0)
                    {
                        // character.Exp += quest.RewardExp;
                    }
                    if (quest.RewardItem1 > 0)
                    {
                        character.ItemManager.AddItem(quest.RewardItem1, quest.RewardItem1Count);
                    }
                    if (quest.RewardItem2 > 0)
                    {
                        character.ItemManager.AddItem(quest.RewardItem2, quest.RewardItem2Count);
                    }
                    if (quest.RewardItem3 > 0)
                    {
                        character.ItemManager.AddItem(quest.RewardItem3, quest.RewardItem3Count);
                    }
                    DBService.Instance.Save();
                    return Result.Success;
                }
                sender.Session.Response.questSubmit.Errormsg = "任务不存在[2]";
                return Result.Failed;
            }
            else
            {
                sender.Session.Response.questSubmit.Errormsg = "任务不存在[3]";
                return Result.Failed;
            }
        }

    }
}
