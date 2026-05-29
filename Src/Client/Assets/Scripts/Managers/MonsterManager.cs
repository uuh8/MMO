using System.Collections.Generic;
using Entities;
using SkillBridge.Message;
using UnityEngine.Events;

namespace Managers
{
    class MonsterManager : Singleton<MonsterManager>
    {
        public Dictionary<int, Monster> MonstersMngr = new Dictionary<int, Monster>();

        public UnityAction<Monster> OnMonsterEnter;
        public UnityAction<Monster> OnMonsterLeave;

        public void Init() { }

        public void AddMonster(NMonsterInfo info)
        {
            Monster monster = new Monster(info);
            MonstersMngr[info.EntityId] = monster;
            EntityManager.Instance.AddEntity(monster);

            if (OnMonsterEnter != null)
                OnMonsterEnter(monster);
        }

        public void RemoveMonster(int entityId)
        {
            if (!MonstersMngr.ContainsKey(entityId))
                return;

            Monster monster = MonstersMngr[entityId];
            EntityManager.Instance.RemoveEntity(monster.Info.Entity);

            if (OnMonsterLeave != null)
                OnMonsterLeave(monster);

            MonstersMngr.Remove(entityId);
        }

        public void Clear()
        {
            MonstersMngr.Clear();
        }
    }
}