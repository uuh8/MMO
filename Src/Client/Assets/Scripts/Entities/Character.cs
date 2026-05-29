using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Data;
using SkillBridge.Message;
using UnityEngine;

namespace Entities
{
    public class Character : Entity
    {
        public NCharacterInfo Info;
        public CharacterDefine Define;

        public int Id
        {
            get { return this.Info.Id; }
        }

        public string Name
        {
            get
            {
                if (this.Info.Type == CharacterType.Player)
                    return this.Info.Name;
                else
                    return this.Define.Name;
            }
        }

        /// <summary>
        /// 判断是不是角色（不是怪物）
        /// </summary>
        public bool IsPlayer
        {
            get 
            { 
                return this.Info.Type == CharacterType.Player; 
            }
        }
        /// <summary>
        /// 是不是当前玩家（不是其他玩家）
        /// </summary>
        public bool IsCurrentPlayer
        {
            get
            {
                if (!IsPlayer) return false;
                return this.Info.Id == Models.User.Instance.CurrentCharacter.Id;
            }
        }

        public Character(NCharacterInfo info) : base(info.Entity)
        {
            this.Info = info;
            this.Define = DataManager.Instance.Characters[info.ConfigId];
        }

        #region 方法封装 注意这些方法都是逻辑状态，不直接操纵 Unity Transform
        // 向前移动
        public void MoveForward()
        {
            this.speed = this.Define.Speed;
        }
        // 向后移动
        public void MoveBack()
        {
            this.speed = this.Define.Speed;
        }
        // 停止
        public void Stop()
        {
            this.speed = 0;
        }
        // 设置方向
        public void SetDirection(Vector3Int direction)
        {
            this.direction = direction;
        }
        // 设置位置
        public void SetPosition(Vector3Int position)
        {
            this.position = position;
        }
        #endregion
    }
}
