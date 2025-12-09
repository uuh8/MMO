using Entities;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*世界元素管理器：维护世界元素（添加或者删除）*/
public class UIWorldElementManager : MonoSingleton<UIWorldElementManager>
{
    public GameObject nameBarPrefab;    // 角色血条
    public GameObject npcStatusPrefab;

    // 用一个字典管理所有 WorldElement
    private Dictionary<Transform, GameObject> elementNames = new Dictionary<Transform, GameObject>();
    private Dictionary<Transform, GameObject> elementStatus = new Dictionary<Transform, GameObject>();

    // Use this for initialization
    protected override void OnStart()
    {
        nameBarPrefab.SetActive(false);
    }

    #region 血条相关
    /// <summary>
    /// 添加/移除角色血条
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="character"></param>
    public void AddCharacterNameBar(Transform owner, Character character)
    {
        GameObject goNameBar = Instantiate(nameBarPrefab, this.transform);
        goNameBar.name = "NameBar" + character.entityId;
        goNameBar.GetComponent<UIWorldElement>().owner = owner;
        goNameBar.GetComponent<UINameBar>().character = character;
        goNameBar.SetActive(true);
        this.elementNames[owner] = goNameBar;
    }
    public void RemoveCharacterNameBar(Transform owner)
    {
        if (this.elementNames.ContainsKey(owner))
        {
            Destroy(this.elementNames[owner]);
            this.elementNames.Remove(owner);
        }
    }
    #endregion

    #region 状态相关
    /// <summary>
    /// 
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="character"></param>
    public void AddNpcQuestStatus(Transform owner, NpcQuestStatus status)
    {
        if (this.elementStatus.ContainsKey(owner))
        {
            elementStatus[owner].GetComponent<UIQuestStatus>().SetQuestStatus(status);
        }
        else
        {
            GameObject go = Instantiate(npcStatusPrefab, this.transform);
            go.name = "NpcQuestStatus" + owner.name;
            go.GetComponent<UIWorldElement>().owner = owner;
            go.GetComponent<UIQuestStatus>().SetQuestStatus(status);
            go.SetActive(true);
            this.elementStatus[owner] = go;
        }

    }
    public void RemoveNpcQuestStatus(Transform owner)
    {
        if (this.elementStatus.ContainsKey(owner))
        {
            Destroy(this.elementStatus[owner]);
            this.elementStatus.Remove(owner);
        }
    }
    #endregion
}
