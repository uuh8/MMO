using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Data
{
    public class NpcDefine
    {
        public enum NpcType
        {
            None = 0,
            Functional = 1,
            Task = 2,
        }
        public enum NpcFunction
        {
            None = 0,
            InvokeShop = 1,
            InvokeInsrance = 2,
        }
        public int ID { get; set; }
        public string Name { get; set; }
        public string Descript { get; set; }
        public NVector3 Position { get; set; }
        public NpcType Type { get; set; }
        public NpcFunction Function { get; set; }
        public int Param { get; set; }
    }
}
