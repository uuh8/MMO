using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Network;
using UnityEngine;
using UnityEngine.Events;
using Entities;
using SkillBridge.Message;

namespace Managers
{
    class CharacterManager : Singleton<CharacterManager>, IDisposable
    {
        public Dictionary<int, Character> CharactersMngr = new Dictionary<int, Character>();

        // 事件
        public UnityAction<Character> OnCharacterEnter;
        public UnityAction<Character> OnCharacterLeave;

        public CharacterManager()
        {

        }

        public void Dispose()
        {
        }

        public void Init()
        {

        }

        public void Clear()
        {
            this.CharactersMngr.Clear();
        }

        /// <summary>
        /// 添加角色到管理器
        /// </summary>
        /// <param name="cha"></param>
        public void AddCharacter(SkillBridge.Message.NCharacterInfo cha)
        {
            Debug.LogFormat("[CharacterManager] AddCharacter:{0}:{1} Map:{2} Entity:{3}", cha.Id, cha.Name, cha.mapId, cha.Entity.String());

            Character character = new Character(cha);
            this.CharactersMngr[cha.Id] = character;
            EntityManager.Instance.AddEntity(character); // 角色也是entity，因此要加入entity管理器中

            if (OnCharacterEnter != null)
            {
                OnCharacterEnter(character);
            }
        }
        /// <summary>
        /// 从管理器中移除角色
        /// </summary>
        /// <param name="characterId"></param>
        public void RemoveCharacter(int characterId)
        {
            Debug.LogFormat("[CharacterManager] RemoveCharacter:{0}", characterId);
            if (this.CharactersMngr.ContainsKey(characterId))
            {
                EntityManager.Instance.RemoveEntity(this.CharactersMngr[characterId].Info.Entity);
                if(OnCharacterLeave != null)
                {
                    OnCharacterLeave(this.CharactersMngr[characterId]);
                }
                this.CharactersMngr.Remove(characterId);
            }
        }
    }
}
