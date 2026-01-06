using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Services;
using SkillBridge.Message;

public class UIRegister : MonoBehaviour {

    public InputField username;
    public InputField password;
    public InputField passwordConfirm;
    public Button buttonRegister;

    public GameObject loginPanel;   // 注册完后返回登录界面
    public GameObject registerPanel;

    void Start () {
        UserService.Instance.OnRegister = this.OnRegister;
    }
    void Update () {
		
	}

    void OnRegister(Result result, string msg)
    {
        if (result == Result.Success)
        {
            MessageBox.Show("注册成功!");

            // 切回登录界面
            registerPanel.SetActive(false);
            loginPanel.SetActive(true);


            // 清空输入框，避免下次打开残留
            username.text = "";
            password.text = "";
            passwordConfirm.text = "";
            return;
        }

        // 失败提示
        MessageBox.Show(string.Format("[UIRegister] 注册失败：{0}", msg));
    }

    //直接给Unity中的组件绑定的方法
    public void OnClickRegister()
    {
        if (string.IsNullOrEmpty(this.username.text))
        {
            MessageBox.Show("请输入账号");
            return;
        }
        if (string.IsNullOrEmpty(this.password.text))
        {
            MessageBox.Show("请输入密码");
            return;
        }
        if (string.IsNullOrEmpty(this.passwordConfirm.text))
        {
            MessageBox.Show("请输入确认密码");
            return;
        }
        if (this.password.text != this.passwordConfirm.text)
        {
            MessageBox.Show("两次输入的密码不一致");
            return;
        }
        SoundManager.Instance.PlaySound(SoundDefine.SFX_UI_Click);
        UserService.Instance.SendRegister(this.username.text, this.password.text);
    }
}
