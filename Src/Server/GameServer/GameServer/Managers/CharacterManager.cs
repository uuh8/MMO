using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Entities;
using SkillBridge.Message;

namespace GameServer.Managers
{
    class CharacterManager : Singleton<CharacterManager>
    {
        public Dictionary<int, Character> Characters = new Dictionary<int, Character>();

        public CharacterManager()
        {

        }

        public void Init()
        {

        }

        public void Clear()
        {
            this.Characters.Clear();
        }
        
        public Character AddCharacter(TCharacter cha)
        {
            // 使用数据库中的数据创建实体(实体对象是在“进入游戏”这个过程中创建的）
            Character character = new Character(CharacterType.Player, cha);
            this.Characters[cha.ID] = character;
            return character;
        }
        public void RemoveCharacter(int characterId)
        {
            // 离开游戏会把角色从实体字典中删除，保证游戏服务器上都是"在线的"玩家
            this.Characters.Remove(characterId);
        }
    }
}
