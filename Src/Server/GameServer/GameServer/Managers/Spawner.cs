using Common;
using Common.Data;
using GameServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class Spawner
    {
        public SpawnRuleDefine Define { get; set; }
        private Map Map;

        private float spawnTime = 0;   // 刷新时间
        private float unspawnTime = 0; // 消失时间
        private bool spawned = false;
        private SpawnPointDefine spawnPoint = null; // 用于保存当前刷怪点

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="define"></param>
        /// <param name="map"></param>
        public Spawner(SpawnRuleDefine define,  Map map)
        {
            this.Define = define;
            this.Map = map;


            // 1) 先取出当前地图的 SpawnPoint 字典
            if (!DataManager.Instance.SpawnPoints.TryGetValue(this.Map.ID, out var mapSpawnPoints))
            {
                Log.ErrorFormat("[Spawner] Map[{0}] 没有 SpawnPointDefine 数据 (SpawnPoints字典缺少该MapID)", this.Map.ID);
                return;
            }

            // 校验该地图中有没有刷怪点
            if (DataManager.Instance.SpawnPoints.ContainsKey(this.Map.ID))
            {
                // 从该地图中找到具体的刷怪点
                if (DataManager.Instance.SpawnPoints[this.Map.ID].ContainsKey(this.Define.SpawnPoint))
                {
                    spawnPoint = DataManager.Instance.SpawnPoints[this.Map.ID][this.Define.SpawnPoint];
                }
                else
                {
                    Log.ErrorFormat("[Spawner] SpawnRule[{0}] SpawnPoint[{1}] 不存在", this.Define.ID, this.Define.SpawnPoint);
                }
            }
        }
    
        public void Update()
        {
            // 每一帧都判断能不能刷
            if (this.CanSpawn())
            {
                this.Spawn();
            }
        }

        bool CanSpawn()
        {
            // 是否已经刷过
            if (this.spawned)
                return false;
            // unspawnTime表示该怪物上次被kill的时间，被kill后需要SpawnPeriod再刷新
            if (this.unspawnTime + this.Define.SpawnPeriod > Time.time)
                return false;

            return true;
        }

        public void Spawn()
        {
            this.spawned = true;
            Log.InfoFormat("[Spawner] Map [{0}] Spawn[{1}] : Mon:{2} , Lv:{3} At Point {4}", this.Define.MapID, this.Define.ID, this.Define.SpawnMonID, this.Define.SpawnLevel, this.Define.SpawnPoint);
            // 刷怪 
            this.Map.MonsterManager.Create(
                this.Define.SpawnMonID, 
                this.Define.SpawnLevel, 
                this.spawnPoint.Position, 
                this.spawnPoint.Direction);
        }
    }
}
