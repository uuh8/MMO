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
        // “逻辑坐标系”三件套。这三个数据是客户端逻辑中实际处理的数据，当服务端发来数据后，需要将发来的这些NEntity中的数据赋值给这三个数据,也就是 SetEntityData 做的事情
        public Vector3Int position;
        public Vector3Int direction;
        public int speed;

        /// <summary>
        /// 从协议层（protobuf）同步过来的结构（权威/最新的网络快照）。
        /// 注意：NEntity 是“数据载体”（DTO），不要把运行时逻辑塞到里面。
        /// </summary>
        private NEntity entityData;
        /// <summary>
        /// 缓存协议层的镜像
        /// - get：返回当前缓存的网络镜像；
        /// - set：替换镜像并“落地”到逻辑三元组（position/direction/speed）。
        /// 这样能保证：一旦收到服务器/网络层的新数据，本地逻辑层立即对齐。
        /// </summary>
        public NEntity EntityData
        {
            get 
            {
                UpdateEntityData();
                return entityData; 
            }
            set
            {
                entityData = value;
                this.SetEntityData(value);
            }
        }

        /// <summary>
        /// 构造时注入一份网络层的 NEntity：
        /// - 保存 Id；
        /// - 缓存镜像；
        /// - 初始化逻辑三元组（position/direction/speed）。
        /// </summary>
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
            if (this.speed != 0)
            {
                // 将逻辑方向转为 Vector3 做乘法
                Vector3 dir = this.direction;
                // 逻辑积分：每个时间步累加“这一小段应该移动的距离”。
                // 这里除以 100f 是逻辑→世界单位缩放（SCALE=100）。
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
