using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Common;

namespace GameServer.Services
{
    class DBService : Singleton<DBService>
    {
        ExtremeWorldEntities entities;  //  Entity Framework 的上下文对象（DbContext）用于与数据库进行交互

        public ExtremeWorldEntities Entities
        {
            get { return this.entities; }
        }

        public void Init()
        {
            entities = new ExtremeWorldEntities();
        }

        public void Save()
        {
            // 用异步保存，表示不用保存完成再返回而是立即返回
            // 因为db保存很慢会影响性能
            // 此处还可以做优化，间隔一定的时间再进行以此db保存，代价是如果服务器崩溃，用户会丢失间隔时间期间的数据
            entities.SaveChangesAsync();
        }
    }
}
