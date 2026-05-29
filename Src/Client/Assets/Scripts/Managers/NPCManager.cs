using Common.Data;
using Models;
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
        private NpcController currentNearestNpc = null;

        private List<NpcController> npcsInRange = new List<NpcController>();
        private Dictionary<NpcFunction, NpcActionHandler> eventMap = new Dictionary<NpcFunction, NpcActionHandler>();   // NPC的功能信息
        private Dictionary<int, Vector3> npcPositions = new Dictionary<int, Vector3>();     // NPC的位置信息

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
            // 任务优先：先检查有没有任务相关交互
            if (DoTaskInteractive(npc))
            {
                return true;
            }
            // 没有任务：再走功能分发
            else if (npc.Type == NpcDefine.NpcType.Functional)
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

        #region npc位置相关
        /// <summary>
        /// 设置NPC的位置
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="pos"></param>
        internal void UpdateNpcPosition(int npc, Vector3 pos)
        {
            this.npcPositions[npc] = pos;
        }
        /// <summary>
        /// 获取NPC的位置
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        internal Vector3 GetNpcPosition(int npc)
        {
            return this.npcPositions[npc];
        }

        public void OnNpcEnterRange(NpcController npc)
        {
            if (!npcsInRange.Contains(npc))
                npcsInRange.Add(npc);
            RefreshNearestNpc();
        }
        public void OnNpcLeaveRange(NpcController npc)
        {
            npcsInRange.Remove(npc);
            RefreshNearestNpc();
        }

        private void RefreshNearestNpc()
        {
            if (npcsInRange.Count == 0)
            {
                currentNearestNpc = null;
                UIWorldElementManager.Instance.HideInteractTip();
                return;
            }
            // 取距离玩家最近的那个
            var playerPos = User.Instance.CurrentCharacterObject.transform.position;
            NpcController nearest = null;
            float minDist = float.MaxValue;
            foreach (var n in npcsInRange)
            {
                float d = (n.transform.position - playerPos).sqrMagnitude; // 用sqrMagnitude避免开方
                if (d < minDist) { 
                    minDist = d; 
                    nearest = n; 
                }
            }
            if (nearest != currentNearestNpc)
            {
                currentNearestNpc = nearest;
                UIWorldElementManager.Instance.ShowInteractTip(currentNearestNpc.transform);
            }
        }
        #endregion

        // 由 PlayerInputController 在 Update 里调用
        public void OnInteractKeyPressed()
        {
            currentNearestNpc?.TryInteract();
        }
    }
}