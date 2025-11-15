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

        /// <summary>
        /// 添加角色到管理器
        /// </summary>
        /// <param name="cha"></param>
        public void AddCharacter(NCharacterInfo cha)
        {
            Debug.LogFormat("[CharacterManager] AddCharacter:{0}:{1} Map:{2} Entity:{3}", cha.Id, cha.Name, cha.mapId, cha.Entity.String());

            Character character = new Character(cha);   // 网络层 → 实体层
            CharactersMngr[cha.Id] = character;          // 加入角色管理器
            EntityManager.Instance.AddEntity(character); // 加入entity管理器
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

            if (CharactersMngr.ContainsKey(characterId))
            {
                EntityManager.Instance.RemoveEntity(CharactersMngr[characterId].Info.Entity);
                if(OnCharacterLeave != null)
                {
                    OnCharacterLeave(CharactersMngr[characterId]);
                }
                CharactersMngr.Remove(characterId);
            }
        }
        public void Clear()
        {
            int[] keys = this.CharactersMngr.Keys.ToArray();
            foreach(var key in keys)
            {
                this.RemoveCharacter(key);
            }
            this.CharactersMngr.Clear();
        }
    }
}
