using Common.Data;
using SkillBridge.Message;
using UnityEngine;

namespace Entities
{
    public class Monster : Entity
    {
        public NMonsterInfo Info;
        public MonsterDefine Define;

        public int Id
        {
            get { return this.Info.EntityId; }
        }

        public string Name
        {
            get { return this.Define.Name; }
        }

        public Monster(NMonsterInfo info) : base(info.Entity)
        {
            this.Info = info;
            this.Define = DataManager.Instance.Monsters[info.ConfigId];
        }
    }
}