using Common.Data;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Services;

namespace Models
{
    /// <summary>
    /// User类的作用是存储与当前用户相关的数据和状态，并提供访问和管理这些数据的功能。
    /// </summary>
    class User : Singleton<User>
    {
        /*用于存储从服务器返回的当前用户的基本信息。*/
        private NUserInfo userInfo;
        public NUserInfo Info { get { return userInfo; } }
        public NCharacterInfo CurrentCharacter { get; set; }
        public NTeamInfo TeamInfo { get; set; } // 角色当前队伍

        public MapDefine CurrentMapData { get; set; }   // 当前在哪一张地图
        public PlayerInputController CurrentCharacterObject { get; set; }  // 当前角色对象

        public System.Action OnGoldChanged;

        //初始化或更新用户信息，例如从服务器登录成功后，调用此方法将服务器返回的用户信息设置到本地实例中
        public void SetupUserInfo(SkillBridge.Message.NUserInfo info)
        {
            this.userInfo = info;
        }

        public void AddGold(int gold)
        {
            this.CurrentCharacter.Gold += gold;
            Debug.Log($"[User] AddGold: Gold={CurrentCharacter.Gold}, ListenerCount={OnGoldChanged?.GetInvocationList().Length ?? 0}");
            OnGoldChanged?.Invoke();
        }

        public int CurrentRide = 0;
        internal void Ride(int id)
        {
            if(CurrentRide != id)
            {
                CurrentRide = id;
                CurrentCharacterObject.SendEntityEvent(EntityEvent.Ride, CurrentRide);
            }
            else
            {
                CurrentRide = 0;
                CurrentCharacterObject.SendEntityEvent(EntityEvent.Ride, 0);
            }
        }
    }
}
