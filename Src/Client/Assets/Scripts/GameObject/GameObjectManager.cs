using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Entities;
using Services;
using SkillBridge.Message;
using Models;
using Managers;

public class GameObjectManager : MonoBehaviour
{

    Dictionary<int, GameObject> Characters = new Dictionary<int, GameObject>();

    // Use this for initialization
    void Start()
    {
        StartCoroutine(InitGameObjects());
        CharacterManager.Instance.OnCharacterEnter += OnCharacterEnter; // 订阅事件
    }

    private void OnDestroy()
    {
        CharacterManager.Instance.OnCharacterEnter = null;
    }

    /// <summary>
    /// 角色进入的初始化逻辑
    /// </summary>
    /// <param name="cha"></param>
    void OnCharacterEnter(Character cha)
    {
        CreateCharacterObject(cha); 
    }

    /// <summary>
    /// 通过一个协程查找当前场景中所有的角色，对每个角色创建游戏对象
    /// </summary>
    /// <returns></returns>
    IEnumerator InitGameObjects()
    {
        foreach (var cha in CharacterManager.Instance.Characters.Values)
        {
            CreateCharacterObject(cha);
            yield return null;
        }
    }

    /// <summary>
    /// 创建单个角色
    /// </summary>
    /// <param name="character"></param>
    private void CreateCharacterObject(Character character)
    {
        // 只有角色不存在的时候才创建
        if (!Characters.ContainsKey(character.Info.Id) || Characters[character.Info.Id] == null)
        {
            // 使用 Resloader 资源加载器加载配置表资源
            Object obj = Resloader.Load<Object>(character.Define.Resource);
            if(obj == null)
            {
                Debug.LogErrorFormat("Character[{0}] Resource[{1}] not existed.",character.Define.TID, character.Define.Resource);
                return;
            }

            // 1. 实例化
            GameObject go = (GameObject)Instantiate(obj);
            go.name = "Character_" + character.Info.Id + "_" + character.Info.Name;

            // 2. 将 “服务器返回到客户端的坐标（实体坐标）” 转变为 “世界坐标”,才能显示在游戏当中
            go.transform.position = GameObjectTool.LogicToWorld(character.position);
            go.transform.forward = GameObjectTool.LogicToWorld(character.direction);
            Characters[character.Info.Id] = go;

            // 3. 给 EntityController 和 PlayerInputController 赋值
            EntityController ec = go.GetComponent<EntityController>();
            PlayerInputController pc = go.GetComponent<PlayerInputController>();
            if (ec != null)
            {
                ec.entity = character;
                ec.isPlayer = character.IsPlayer;
            }
            if (pc != null)
            {
               
                if (character.Info.Id == Models.User.Instance.CurrentCharacter.Id)
                {
                    User.Instance.CurrentCharacterObject = go;
                    MainPlayerCamera.Instance.player = go;
                    pc.enabled = true;
                    pc.character = character;
                    pc.entityController = ec;
                }
                else
                {
                    pc.enabled = false;
                }
            }
            UIWorldElementManager.Instance.AddCharacterNameBar(go.transform, character);
        }
    }
}
