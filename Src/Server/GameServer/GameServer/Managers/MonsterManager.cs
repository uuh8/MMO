using GameServer.Entities;
using GameServer.Models;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class MonsterManager
    {
        private Map Map;
        public Dictionary<int, Monster> Monsters = new Dictionary<int, Monster>();
        public void Init(Map map)
        {
            this.Map = map;
        }

        /// <summary>
        /// 创建一只怪物
        /// </summary>
        /// <param name="spawnMonID"></param>
        /// <param name=""></param>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        internal Monster Create(int spawnMonID, int spawnLevel, NVector3 position, NVector3 direction)
        {
            Monster monster = new Monster(spawnMonID, spawnLevel, position, direction);
            EntityManager.Instance.AddEntity(this.Map.ID, monster);
            monster.Id = monster.entityId;              // 作用不大，因为怪物不是玩家
            monster.Info.EntityId = monster.entityId;
            monster.Info.mapId = this.Map.ID;
            Monsters[monster.Id] = monster;

            this.Map.MonsterEnter(monster); // 地图需要通知给所有玩家

            return monster;
        }
    }
}
