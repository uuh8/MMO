using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Entities;

namespace GameServer.Managers
{
    class EntityManager : Singleton<EntityManager>
    {
        private int idx = 0;
        public List<Entity> AllEntities = new List<Entity>();
        // 区分每个地图的entity
        public Dictionary<int, List<Entity>> MapEntities = new Dictionary<int, List<Entity>>();

        /// <summary>
        /// 添加/移除 实体
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="entity"></param>
        public void AddEntity(int mapId, Entity entity)
        {
            AllEntities.Add(entity);

            // 加入实体管理器，生成唯一id
            entity.EntityData.Id = ++idx;

            if (entity is CharacterBase cb)
            {
                cb.Info.EntityId = entity.EntityData.Id;
            }

            List<Entity> entities = null;
            if(!MapEntities.TryGetValue(mapId, out entities))
            {
                entities = new List<Entity>();
                MapEntities[mapId] = entities;
            }
            entities.Add(entity);
        }
        public void RemoveEntity(int mapId, Entity entity)
        {
            this.AllEntities.Remove(entity);
            this.MapEntities[mapId].Remove(entity);
        }
    }
}
