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
        void OnEntityEvent(EntityEvent @event, int param);
    }
    class EntityManager : Singleton<EntityManager>
    {

        // 维护一个客户端本地的Entity字典
        Dictionary<int, Entity> entities = new Dictionary<int, Entity>();

        // 使用接口实现“事件”，这种实现方式可以让一个接收者可以接收多种事件。
        // EntityManager 只知道"有一个东西实现了这个接口"，完全不知道那个东西是 EntityController，也不需要知道。
        Dictionary<int, IEntityNotify> notifiers = new Dictionary<int, IEntityNotify>();

        // 给EntityController使用的，注册事件的接收者
        public void RegisterEntityChangeNotify(int entityId, IEntityNotify notify)
        {
            this.notifiers[entityId] = notify;
        }

        /// <summary>
        /// 添加/删除 entity
        /// </summary>
        /// <param name="entity"></param>
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
        /// 用 entityId 拿到 Entity 对象
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public Entity GetEntity(int entityId)
        {
            Entity entity = null;
            entities.TryGetValue(entityId, out entity);
            return entity;
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

            if (entity != null)
            {
                // 第一步：更新逻辑数据
                if (entitySync.Entity != null)
                {
                    // 触发setter，把服务端发来的 NEntity 里的 position、direction、speed 三个字段写入 Entity 的逻辑层字段，这一步的本质是：用服务端的权威数据覆盖客户端本地的逻辑状态
                    entity.EntityData = entitySync.Entity;
                }

                // 第二步：通过接口通知表现层
                if (notifiers.ContainsKey(entitySync.Id))
                {
                    // 从 EntityManager 的角度看，它只是在调用"某个实现了 IEntityNotify 接口的对象的 OnEntityChanged 方法
                    // 但实际上这个调用最终落到了 EntityController.OnEntityChanged()，触发了表现层的响应
                    notifiers[entity.entityId].OnEntityChanged(entity);                                 // 通知数据变化
                    notifiers[entity.entityId].OnEntityEvent(entitySync.Event, entitySync.Param);       // 通知状态变化
                }
            }
        }
    }
}