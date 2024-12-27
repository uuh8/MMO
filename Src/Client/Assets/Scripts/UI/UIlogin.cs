using Services;
using SkillBridge.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIlogin : MonoBehaviour
{
    public InputField username;
    public InputField password;
    public Button buttonlogin;
    public Button buttonRegister;

    // Start is called before the first frame update
    void Start()
    {
        UserService.Instance.OnLogin = this.Login;

        buttonlogin.onClick.AddListener(OnClickLogin);
    }
    void Update()
    {
        
    }

    void Login(Result result, string message)
    {
        if (result == Result.Success)
        {
            //��¼�ɹ��������ɫѡ��
            MessageBox.Show("��¼�ɹ�,׼����ɫѡ��" + message,"��ʾ", MessageBoxType.Information);
            SceneManager.Instance.LoadScene("CharSelect");
        }
        else
            MessageBox.Show(message, "����", MessageBoxType.Error);
    }

    //ֱ�Ӹ�Unity�е�����󶨵ķ���
    public void OnClickLogin()
    {
        if (string.IsNullOrEmpty(this.username.text))
        {
            MessageBox.Show("�������˺�");
            return;
        }
        if (string.IsNullOrEmpty(this.password.text))
        {
            MessageBox.Show("����������");
            return;
        }
        UserService.Instance.SendLogin(this.username.text, this.password.text);
    }

}
