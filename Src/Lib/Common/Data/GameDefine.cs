using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Data
{
    /// <summary>
    /// 全局游戏常量
    /// </summary>
    public class GameDefine
    {
        // 背包
        public const int BagMaxItemPerPage = 30;
        // 公会
        public const int GuildMaxMemberCount = 50;      
        public const int GuildNameMaxLength = 12;
        public const int GuildNoticeMaxLength = 200;
        // 聊天
        public const int MaxChatRecoredNums = 20;   // 聊天记录特别少，就拉20条（之前的聊天记录，例如刚上线的时候没有聊天记录，就拉之前的20条）
        public const int MaxChatRecoredTime = 600;  // 聊天记录特别多，就拉10分钟已内的聊天
    }
}
