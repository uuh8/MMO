using GameServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    /// <summary>
    /// 刷怪管理器
    /// </summary>
    class SpawnManager
    {
        private List<Spawner> Rules = new List<Spawner>();
        private Map Map;

        public void Init(Map map)
        {
            this.Map = map;
            if (DataManager.Instance.SpawnRules.ContainsKey(map.Define.ID))
            {
                foreach(var define in DataManager.Instance.SpawnRules[map.Define.ID].Values)
                {
                    // 为每一条“刷怪规则Rule”创建一个刷怪器Spawner，并添加到规则管理器Rules中
                    this.Rules.Add(new Spawner(define, this.Map));
                }
            }
        }

        public void Update()
        {
            if (Rules.Count == 0)
                return;

            for(int i = 0; i < this.Rules.Count; i++)
            {
                this.Rules[i].Update();
            }
        }
    }
}
