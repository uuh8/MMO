using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputBox : MonoBehaviour
{
    static Object cacheObject = null;

    public static UIInputBox Show(string message, string title = "", string btnOK = "", string btnCancel = "", string emptyTips = "")
    {
        if (cacheObject == null)
        {
            cacheObject = Resloader.Load<Object>("UI/UIInputBox");
        }

        GameObject go = (GameObject)GameObject.Instantiate(cacheObject); //实例化消息框对象
        UIInputBox inputBox = go.GetComponent<UIInputBox>();
        inputBox.Init(title, message, btnOK, btnCancel, emptyTips);
        return inputBox;
    }
}
