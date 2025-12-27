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
        // Cache=true 的含义基本就是：第一次加载实例后不销毁，后面反复 Show/Hide 复用同一个实例；
        // Cache=false 则通常是：每次打开都 Instantiate，需要时才存在，关闭时 Destroy（或至少不保留引用，等 GC/资源释放）。
        this.UIResources.Add(typeof(UISetting), new UIElement() { Resources = "UI/UISetting", Cache = true });
        this.UIResources.Add(typeof(UIBag), new UIElement() { Resources = "UI/UIBag", Cache = false });
        this.UIResources.Add(typeof(UIShop), new UIElement() { Resources = "UI/UIShop", Cache = false });
        this.UIResources.Add(typeof(UICharEquip), new UIElement() { Resources = "UI/UICharEquip", Cache = false });
        this.UIResources.Add(typeof(UIQuestDialog), new UIElement() { Resources = "UI/UIQuestDialog", Cache = false });
        this.UIResources.Add(typeof(UIQuestSystem), new UIElement() { Resources = "UI/UIQuestSystem", Cache = false });
        this.UIResources.Add(typeof(UIFriends), new UIElement() { Resources = "UI/UIFriends", Cache = false });
        this.UIResources.Add(typeof(UIGuild), new UIElement() { Resources = "UI/Guild/UIGuild", Cache = false });
        this.UIResources.Add(typeof(UIGuildList), new UIElement() { Resources = "UI/Guild/UIGuildList", Cache = false });
        this.UIResources.Add(typeof(UIGuildPopNoGuild), new UIElement() { Resources = "UI/Guild/UIGuildPopNoGuild", Cache = false });
        this.UIResources.Add(typeof(UIGuildPopCreate), new UIElement() { Resources = "UI/Guild/UIGuildPopCreate", Cache = false });
        this.UIResources.Add(typeof(UIGuildApplyList), new UIElement() { Resources = "UI/Guild/UIGuildApplyList", Cache = false });
        this.UIResources.Add(typeof(UIPopCharMenu), new UIElement() { Resources = "UI/UIPopCharMenu", Cache = false });
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
