using Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGuildPopCreate : UIWindow
{
    public InputField guildName;
    public InputField guildNotice;

    // Start is called before the first frame update
    void Start()
    {
        GuildService.Instance.OnGuildCreateResult += OnGuildCreated;
    }

    /// <summary>
    /// 点击创建工会
    /// </summary>
    public override void OnYesClick()
    {
        // 校验合法性
        if (string.IsNullOrEmpty(guildName.text))
        {
            MessageBox.Show("请输入工会名称", "错误", MessageBoxType.Error);
            return;
        }
        if(guildName.text.Length < 4 || guildName.text.Length > 10)
        {
            MessageBox.Show("工会名称为4-10个字符", "错误", MessageBoxType.Error);
            return;
        }
        if (string.IsNullOrEmpty(guildNotice.text))
        {
            MessageBox.Show("请输入工会宣言", "错误", MessageBoxType.Error);
            return;
        }
        if (guildNotice.text.Length < 3 || guildNotice.text.Length > 50)
        {
            MessageBox.Show("工会宣言需3-50个字符", "错误", MessageBoxType.Error);
            return;
        }

        GuildService.Instance.SendGuildCreate(guildName.text, guildNotice.text);
    }

    /// <summary>
    /// 服务端消息响应
    /// </summary>
    /// <param name="result"></param>
    private void OnGuildCreated(bool result)
    {
        if (result)
        {
            // 服务端返回结果后才关闭UI界面（不然每次还需要重新点击工会按钮）
            this.Close(WindowResult.Yes);
        }
    }
}
