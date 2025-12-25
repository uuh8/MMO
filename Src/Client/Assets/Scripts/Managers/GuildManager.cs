using Models;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Managers
{
    public class GuildManager : Singleton<GuildManager>
    {
        public NGuildInfo guildInfo;

        public NGuildMemberInfo myMemberInfo;   // “本客户端”的角色在工会中的成员信息

        // 判断本客户端的角色有没有加入工会
        public bool HasGuild
        {
            get {return this.guildInfo != null;}
        }
        public void Init(NGuildInfo guild)
        {
            this.guildInfo = guild;
            // 初始化 myMemberInfo
            if (guild == null)
            {
                // 如果没有公会
                myMemberInfo = null;
                return;
            }
            foreach (var mem in guild.Members)
            {
                // 如果有公会 
                if(mem.characterId == User.Instance.CurrentCharacter.Id)
                {
                    myMemberInfo = mem;
                    break;
                }
            }
        }

        public void ShowGuildUI()
        {
            if (this.HasGuild)
                UIManager.Instance.Show<UIGuild>();
            else
            {
                UIGuildPopNoGuild win = UIManager.Instance.Show<UIGuildPopNoGuild>();
                win.OnClose += PopNoGuild_OnClose;
            }
        }

        private void PopNoGuild_OnClose(UIWindow sender, UIWindow.WindowResult result)
        {
            if(result == UIWindow.WindowResult.Yes)
            {
                // 创建工会
                UIManager.Instance.Show<UIGuildPopCreate>();
            }
            else if(result == UIWindow.WindowResult.No)
            {
                // 加入工会
                UIManager.Instance.Show<UIGuildList>();
            }
        }
    }

}
