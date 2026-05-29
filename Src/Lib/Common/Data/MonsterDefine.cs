using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Data
{
    public class MonsterDefine
    {
        public int TID { get; set; }
        public string Name { get; set; }
        public string Resource { get; set; } // 对应 M1001
        public int Speed { get; set; }
    }
}