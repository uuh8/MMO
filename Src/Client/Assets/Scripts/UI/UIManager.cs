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

    // 记录ui打开顺序的栈
    private Stack<Type> openStack = new Stack<Type>();

    // 构造函数（写在构造函数中目的是第一时间绘制出来）
    public UIManager()
    {
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
        this.UIResources.Add(typeof(UIRide), new UIElement() { Resources = "UI/UIRide", Cache = false });
        this.UIResources.Add(typeof(UISystemConfig), new UIElement() { Resources = "UI/UISystemConfig", Cache = false });
    }

    public T Show<T>()
    {
        // 窗口显示的时候播放一个窗口打开的声音
        SoundManager.Instance.PlaySound(SoundDefine.SFX_UI_Win_Open);

        Type type = typeof(T);
        if (!this.UIResources.ContainsKey(type))
            return default(T);

        UIElement info = this.UIResources[type];
        if (info.Instance != null)
        {
            info.Instance.SetActive(true);
        }
        else
        {
            // 从资源中加载
            UnityEngine.Object prefab = Resources.Load(info.Resources);
            if (prefab == null)
            {
                return default(T);
            }
            // 实例化
            info.Instance = (GameObject)GameObject.Instantiate(prefab);
        }

        // 入栈：记录打开顺序（避免重复入栈）
        if (!openStack.Contains(type))
            openStack.Push(type);

        return info.Instance.GetComponent<T>();
    }

    public void Close(Type type)
    {
        if (!this.UIResources.ContainsKey(type))
            return;

        UIElement info = this.UIResources[type];
        if (info.Cache)
            info.Instance.SetActive(false);
        else
        {
            GameObject.Destroy(info.Instance);
            info.Instance = null;
        }

        // 出栈：从打开记录里移除
        // Stack 不支持直接删除中间元素，重建一个去掉该类型的栈
        var temp = new Stack<Type>();
        foreach (var t in openStack)
            if (t != type) temp.Push(t);
        openStack.Clear();
        foreach (var t in temp)
            openStack.Push(t);
    }

    public void Close<T>()
    {
        this.Close(typeof(T));
    }

    // 关闭最近打开的面板（ESC 键调用）
    public bool CloseTop()
    {
        if (openStack.Count > 0)
        {
            Type top = openStack.Pop();
            this.Close(top);
            return true;
        }
        return false;
    }
}
