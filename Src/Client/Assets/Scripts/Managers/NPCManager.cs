using Common.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Common.Data.NpcDefine;

namespace Managers
{
    class NPCManager: Singleton<NPCManager>
    {
        public delegate bool NpcActionHandler(NpcDefine npc);

        Dictionary<NpcFunction, NpcActionHandler> eventMap = new Dictionary<NpcFunction, NpcActionHandler>();

        public void RegisterNpcEvent(NpcFunction function, NpcActionHandler action)
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

        public NpcDefine GetNpcDefine(int npcID)
        {
            NpcDefine npc = null;
            DataManager.Instance.Npcs.TryGetValue(npcID, out npc);
            return npc;
        }

        /// <summary>
        /// 判断 npc 是否存在并交互
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        public bool Interactive(int npcId)
        {
            if (DataManager.Instance.Npcs.ContainsKey(npcId))
            {
                var npc = DataManager.Instance.Npcs[npcId];
                return Interactive(npc);
            }
            return false;
        }
        /// <summary>
        /// npc 交互函数，负责分发
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public bool Interactive(NpcDefine npc)
        {
            if(DoTaskInteractive(npc))
            {
                return true;
            }
            else if(npc.Type == NpcDefine.NpcType.Functional)
            {
                return DoFunctionInteractive(npc);
            }
            return false;
        }
        /// <summary>
        /// 任务npc的交互
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        private bool DoTaskInteractive(NpcDefine npc)
        {
            var status = QuestManager.Instance.GetQuestStatusByNpc(npc.ID);
            if (status == NpcQuestStatus.None)
                return false;

            return QuestManager.Instance.OpenNpcQuest(npc.ID);
        }
        /// <summary>
        /// 功能npc的交互
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        private bool DoFunctionInteractive(NpcDefine npc)
        {
            if (npc.Type != NpcType.Functional)
                return false;

            if (!eventMap.ContainsKey(npc.Function))
            {
                return false;
            }

            return eventMap[npc.Function](npc);
        }
    }
}