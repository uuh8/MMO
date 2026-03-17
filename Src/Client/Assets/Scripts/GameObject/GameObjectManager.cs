using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Entities;
using Services;
using SkillBridge.Message;
using Models;
using Managers;
using Common.Data;

// 使用单例，使GameObjectManager在切换场景时不被销毁
public class GameObjectManager : MonoSingleton<GameObjectManager>
{
    Dictionary<int, GameObject> Characters = new Dictionary<int, GameObject>();

    // Start是在第一帧Update之前自动调用，OnStart不是生命周期函数，一般是程序员自定义的用来实现自己的初始化逻辑
    protected override void OnStart()
    {
        StartCoroutine(InitGameObjects());  // 启动协程
        CharacterManager.Instance.OnCharacterEnter += OnCharacterEnter; // 订阅事件
        CharacterManager.Instance.OnCharacterLeave += OnCharacterLeave; 
    }

    private void OnDestroy()
    {
        CharacterManager.Instance.OnCharacterEnter -= OnCharacterEnter; // 取消订阅
        CharacterManager.Instance.OnCharacterLeave -= OnCharacterLeave;
    }

    /// <summary>
    /// 通过一个协程查找当前场景中所有的角色，对每个角色创建游戏对象
    /// </summary>
    /// <returns></returns>
    IEnumerator InitGameObjects()
    {
        foreach (var cha in CharacterManager.Instance.CharactersMngr.Values)
        {
            CreateCharacterObject(cha);
            yield return null;
        }
    }

    /// <summary>
    /// 其他角色进入的初始化逻辑 
    /// </summary>
    /// <param name="cha"></param>
    void OnCharacterEnter(Character cha)
    {
        CreateCharacterObject(cha); 
    }
    /// <summary>
    /// 其他角色离开的初始化逻辑
    /// </summary>
    /// <param name="cha"></param>
    void OnCharacterLeave(Character cha)
    {
        if (!Characters.ContainsKey(cha.entityId))
            return;

        // 这不是移除角色的唯一入口，因此需要判空安全删除
        if (Characters[cha.entityId] != null)
        {
            Destroy(Characters[cha.entityId]);
            this.Characters.Remove(cha.entityId);
        }
    }

    /// <summary>
    /// 创建单个角色（玩家自己和其他玩家都用这个）
    /// </summary>
    /// <param name="character"></param>
    private void CreateCharacterObject(Character character)
    {
        // 只有角色不存在的时候才创建
        if (!Characters.ContainsKey(character.entityId) || Characters[character.entityId] == null)
        {
            // 使用 Resloader 资源加载器加载配置表资源
            Object obj = Resloader.Load<Object>(character.Define.Resource); 
            if(obj == null)
            {
                Debug.LogErrorFormat("[GameObjectManager] Character[{0}] Resource[{1}] not existed.",character.Define.TID, character.Define.Resource);
                return;
            }

            // Character 实例化
            GameObject go = (GameObject)Instantiate(obj, this.transform);
            go.name = "Character_" + character.Info.Id + "_" + character.Info.Name;
            Characters[character.entityId] = go;
 
            UIWorldElementManager.Instance.AddCharacterNameBar(go.transform, character);
        }

        // Character 初始化
        this.InitGameObject(Characters[character.entityId], character);
    }
    /// <summary>
    /// 初始化GameObject
    /// </summary>
    /// <param name="go"></param>
    /// <param name="character"></param>
    private void InitGameObject(GameObject go, Character character)
    {
        go.transform.position = GameObjectTool.LogicToWorld(character.position);
        go.transform.forward = GameObjectTool.LogicToWorld(character.direction);

        EntityController ec = go.GetComponent<EntityController>();
        PlayerInputController pc = go.GetComponent<PlayerInputController>();

        if(ec != null)
        {
            ec.entity = character;
            ec.isPlayer = character.IsCurrentPlayer;
            ec.Ride(character.Info.Ride);
        }

        if(pc != null)
        {
            if (character.IsCurrentPlayer)
            {
                // 若这是“自己”，就把自己保存到 User.Instance.CurrentCharacterObject、把相机的跟随目标指向自己，并开启本地输入
                User.Instance.CurrentCharacterObject = pc;
                MainPlayerCamera.Instance.player = go;
                pc.enabled = true;
                pc.character = character;
                pc.entityController = ec;
            }
            else
            {
                // 不是“自己”不启动 PlayerInputController
                pc.enabled = false;
            }
        }
    }

    public RideController LoadRide(int rideId, Transform parent)
    {
        var rideDefine = DataManager.Instance.Rides[rideId];
        Object obj = Resloader.Load<Object>(rideDefine.Resource);
        if(obj == null)
        {
            Debug.LogErrorFormat("[GameObjectManager] Ride[{0}] Resource[{1}] not existed", rideDefine.ID, rideDefine.Resource);
            return null;
        }
        GameObject go = (GameObject)Instantiate(obj, parent);
        go.name = "Ride_" + rideDefine.ID + "_" + rideDefine.Name;
        return go.GetComponent<RideController>();
    }
}
