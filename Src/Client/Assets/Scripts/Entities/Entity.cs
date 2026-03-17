using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Entities
{
    /*父类，逻辑层的抽象实体*/
    public class Entity
    {
        public int entityId;
        // “逻辑层”三件套。这三个数据是客户端逻辑中实际处理的数据，当服务端发来数据后，需要将发来的这些NEntity中的数据赋值给这三个数据,也就是 SetEntityData 做的事情
        public Vector3Int position;
        public Vector3Int direction;
        public int speed;

        private NEntity entityData;
        public NEntity EntityData
        {
            // 网络收到服务器数据，set 把数据落地到逻辑层；
            // 要发包了，get 把逻辑层最新数据取出来。
            get { UpdateEntityData(); return entityData; } // 逻辑 → 网络，发包时用
            set { entityData = value; SetEntityData(value); } // 网络 → 逻辑，收包时用
        }
        public Entity(NEntity entity)
        {
            this.entityId = entity.Id;
            this.entityData = entity;
            this.SetEntityData(entity);
        }

        /// <summary>
        /// 逻辑层位移积分（不是 Unity 的物理世界），并把结果回写到 entityData，保持逻辑层与网络层镜像一致
        /// </summary>
        /// <param name="delta"></param>
        public virtual void OnUpdate(float delta)
        {
            // 每帧根据方向和速度推算下一帧位置，不依赖网络包，让移动看起来连续。
            if (this.speed != 0)
            {
                // 将逻辑方向转为 Vector3 做乘法
                Vector3 dir = this.direction;
                // 逻辑积分：每个时间步累加“这一小段应该移动的距离”。
                // 除以 100f 是逻辑→世界单位缩放（SCALE=100）。
                // RoundToInt 确保仍在整数网格上，便于跨端一致。
                this.position += Vector3Int.RoundToInt(dir * speed * delta / 100f);
            }
            // 将最新的逻辑三元组回抄到协议镜像（方便网络层直接拿 entityData 发包）
            entityData.Position.FromVector3Int(this.position);
            entityData.Direction.FromVector3Int(this.direction);
            entityData.Speed = this.speed;
        }

        /// <summary>
        /// 用网络/服务器来的 NEntity 更新逻辑层 Entity
        /// 和 UpdateEntityData 相反
        /// </summary>
        /// <param name="entity"></param>
        public void SetEntityData(NEntity entity)
        {
            this.position = this.position.FromNVector3(entity.Position);
            this.direction = this.direction.FromNVector3(entity.Direction);
            this.speed = entity.Speed;
        }
        /// <summary>
        /// 把逻辑层的 Entity 的值更新到 NEntity
        /// 和 SetEntityData 相反
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void UpdateEntityData()
        {
            entityData.Position.FromVector3Int(this.position);
            entityData.Direction.FromVector3Int(this.direction);
            entityData.Speed = this.speed;
        }
    }
}
