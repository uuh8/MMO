using Entities;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    interface IEntityNotify
    {
        void OnEntityRemoved();
        void OnEntityChanged(Entity entity);
        void OnEntityEvent(EntityEvent @event);
    }
    class EntityManager : Singleton<EntityManager>
    {
        // 维护一个客户端本地的Entity字典
        Dictionary<int, Entity> entities = new Dictionary<int, Entity>();
        // 使用接口实现“事件”，这种实现方式可以让一个接收者可以接收多种事件
        Dictionary<int, IEntityNotify> notifiers = new Dictionary<int, IEntityNotify>();

        // 给EntityController使用的，注册事件的接收者
        public void RegisterEntityChangeNotify(int entityId, IEntityNotify notify)
        {
            this.notifiers[entityId] = notify;
        }

        public  void AddEntity(Entity entity)
        {
            entities[entity.entityId] = entity;
        }

        public void RemoveEntity(NEntity entity)
        {
            this.entities.Remove(entity.Id);
            if (notifiers.ContainsKey(entity.Id))
            {
                notifiers[entity.Id].OnEntityRemoved();
                notifiers.Remove(entity.Id);
            }
        }

        /// <summary>
        /// 实体同步
        /// </summary>
        /// <param name="entity"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void OnEntitySync(NEntitySync entitySync)
        {
            Entity entity = null;
            entities.TryGetValue(entitySync.Id, out entity);

            // entity != null表示本地有这个entity
            if (entity != null)
            {
                if (entitySync.Entity != null)
                {
                    // 是自己
                    entity.EntityData = entitySync.Entity;
                }
                if (notifiers.ContainsKey(entitySync.Id))
                {
                    notifiers[entity.entityId].OnEntityChanged(entity);             // 通知数据变化
                    notifiers[entity.entityId].OnEntityEvent(entitySync.Event);     // 通知状态变化
                }
            }
        }
    }
}