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
        private Map map;
        public Dictionary<int, Monster> Monsters = new Dictionary<int, Monster>();

        public void Init(Map map)
        {
            this.map = map;
        }

        public void Update()
        {
            foreach (var kv in Monsters)
            {
                Monster monster = kv.Value;
                // 获取地图上所有玩家
                var characters = map.GetAllCharacters();

                // 驱动状态机，拿到需要广播的消息
                MonsterStateSync sync = monster.UpdateAI(characters);

                // 有状态变化才广播，没有变化不发包
                if (sync != null)
                {
                    map.BroadcastMonsterState(sync);
                }
            }
        }

        /// <summary>
        /// 创建一只怪物
        /// </summary>
        /// <param name="spawnMonID"></param>
        /// <param name=""></param>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        internal Monster Create(int spawnMonID, int spawnLevel,
            NVector3 position, NVector3 direction, int spawnPointId)
        {
            Monster monster = new Monster(spawnMonID, spawnLevel, position, direction);
            EntityManager.Instance.AddEntity(this.map.ID, monster);
            monster.Id = monster.entityId;

            // 同步 Entity 数据到 MonsterInfo
            monster.MonsterInfo.EntityId = monster.entityId;
            monster.MonsterInfo.MapId = this.map.ID;
            monster.MonsterInfo.Entity = monster.Info.Entity;
            monster.MonsterInfo.SpawnPointId = spawnPointId;
            monster.InitSpawnPoint(spawnPointId); // 初始化刷怪点配置

            Monsters[monster.Id] = monster;

            this.map.MonsterEnter(monster);
            return monster;
        }
    }
}
