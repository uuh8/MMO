using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using GameServer.Models;

namespace GameServer.Managers
{
    class MapManager : Singleton<MapManager>
    {
        Dictionary<int, Map> Maps = new Dictionary<int, Map>();

        // 初始化注册地图，在服务器启动的时候就会执行
        public void Init()
        {
            foreach (var mapdefine in DataManager.Instance.Maps.Values)
            {
                Map map = new Map(mapdefine);
                this.Maps[mapdefine.ID] = map;  // 交给地图管理器

                Log.InfoFormat("[MapManager] MapManager.Init => Map:{0}:{1}", map.Define.ID, map.Define.Name);
            }
        }

        public Map this[int key]
        {
            get
            {
                return this.Maps[key];
            }
        }

        /// <summary>
        /// 地图更新函数（比如到了一定的时期地图会刷新一个Boss）
        /// </summary>
        public void Update()
        {
            foreach(var map in this.Maps.Values)
            {
                map.Update();
            }
        }
    }
}
