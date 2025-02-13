using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models
{
    class User : Singleton<User> //User类的作用是存储与当前用户相关的数据和状态，并提供访问和管理这些数据的功能。
    {
        /*message NUserInfo {
            int32 id = 1;
            NPlayerInfo player = 2;
        }
        message NPlayerInfo {
            int32 id = 1;
            repeated NCharacterInfo characters = 2;
        }
        message NCharacterInfo {
            int32 id = 1;
            int32 tid = 2;
            string name = 3;
            CHARACTER_TYPE type = 4;
            CHARACTER_CLASS class = 5;
            int32 level = 6;
            int32 mapId = 7;
            NEntity entity = 8;
        }*/

        /*用于存储从服务器返回的当前用户的基本信息。*/
        SkillBridge.Message.NUserInfo userInfo;


        /*Info属性用来访问存储的用户信息。*/
        public SkillBridge.Message.NUserInfo Info
        {
            get { return userInfo; }
        }

        /*初始化或更新用户信息，例如从服务器登录成功后，调用此方法将服务器返回的用户信息设置到本地实例中。*/
        public void SetupUserInfo(SkillBridge.Message.NUserInfo info)
        {
            this.userInfo = info;
        }

        /*用于管理当前登录用户的角色信息*/
        public SkillBridge.Message.NCharacterInfo CurrentCharacter { get; set; }

    }
}
