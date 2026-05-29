using Common.Data;
using GameServer.Core;
using GameServer.Managers;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entities
{
    /*父类*/
    class CharacterBase : Entity
    {

        public int Id { get; set; }
        public string Name { get { return this.Info.Name; } }

        public NCharacterInfo Info;
        public CharacterDefine Define;

        public CharacterBase(Vector3Int pos, Vector3Int dir):base(pos,dir)
        {

        }

        public CharacterBase(CharacterType type, int configId, int level, Vector3Int pos, Vector3Int dir) :
           base(pos, dir)
        {
            this.Info = new NCharacterInfo();
            this.Info.Type = type;
            this.Info.Level = level;
            this.Info.ConfigId = configId;          // 配置表里的id
            this.Info.Entity = this.EntityData;
            this.Info.EntityId = this.entityId;     // 实体id

            if (type == CharacterType.Monster)
            {
                // 怪物不查 Characters 字典，名字由 Monster 子类自己设置
                this.Define = null;
                this.Info.Name = "Monster_" + configId;
            }
            else
            {
                // 玩家正常查 Characters 字典
                this.Define = DataManager.Instance.Characters[configId];
                this.Info.Name = this.Define.Name;
            }
        }
    }
}
