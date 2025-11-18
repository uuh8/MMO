using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager:Singleton<UIManager>
{
    class UIElement
    {
        public string Resources;        // UI的资源路径
        public bool Cache;              // 是否需要缓存
        public GameObject Instance;     // 实例 
    }

    private Dictionary<Type, UIElement> UIResources = new Dictionary<Type, UIElement>();

    // 构造函数（写在构造函数中目的是第一时间绘制出来）
    public UIManager()
    {
        this.UIResources.Add(typeof(UITest), new UIElement() { Resources = "UI/UITest", Cache = true });
    }

    public T Show<T>()
    {
        // SoundManager.Instance.PlaySound("ui_open");
        Type type = typeof(T);
        if (this.UIResources.ContainsKey(type))
        {
            UIElement info = this.UIResources[type];
            if(info.Instance != null)
            {
                info.Instance.SetActive(true);
            }
            else
            {
                // 从资源中加载
                UnityEngine.Object prefab = Resources.Load(info.Resources);
                if(prefab == null)
                {
                    return default(T);
                }
                // 实例化
                info.Instance = (GameObject)GameObject.Instantiate(prefab);
            }
            return info.Instance.GetComponent<T>();
        }
        return default(T);
    }

    public void Close(Type type)
    {
        // SoundManager.Instance.PlaySound("ui_close");
        if (this.UIResources.ContainsKey(type))
        {
            UIElement info = this.UIResources[type];
            if (info.Cache)
            {
                info.Instance.SetActive(false);
            }
            else
            {
                GameObject.Destroy(info.Instance);
                info.Instance = null;
            }
        }
    }
}
