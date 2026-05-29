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
    Dictionary<int, GameObject> EntityObjects = new Dictionary<int, GameObject>();

    // 待初始化的怪物列表
    private List<Monster> pendingPatrolInits = new List<Monster>();

    // Start是在第一帧Update之前自动调用，OnStart不是生命周期函数，一般是程序员自定义的用来实现自己的初始化逻辑
    protected override void OnStart()
    {
        StartCoroutine(InitGameObjects());  // 启动协程
        CharacterManager.Instance.OnCharacterEnter += OnCharacterEnter;
        CharacterManager.Instance.OnCharacterLeave += OnCharacterLeave;

        MonsterManager.Instance.OnMonsterEnter += OnMonsterEnter;
        MonsterManager.Instance.OnMonsterLeave += OnMonsterLeave;

        // 监听场景加载完成事件
        SceneManager.Instance.OnLevelLoaded += OnLevelLoaded;
    }

    private void OnDestroy()
    {
        CharacterManager.Instance.OnCharacterEnter -= OnCharacterEnter;
        CharacterManager.Instance.OnCharacterLeave -= OnCharacterLeave;

        MonsterManager.Instance.OnMonsterEnter -= OnMonsterEnter;
        MonsterManager.Instance.OnMonsterLeave -= OnMonsterLeave;

        SceneManager.Instance.OnLevelLoaded -= OnLevelLoaded;
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

        // 怪物初始化
        foreach (var monster in MonsterManager.Instance.MonstersMngr.Values)
        {
            CreateMonsterObject(monster);
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
        if (!EntityObjects.ContainsKey(cha.entityId))
            return;

        // 这不是移除角色的唯一入口，因此需要判空安全删除
        if (EntityObjects[cha.entityId] != null)
        {
            Destroy(EntityObjects[cha.entityId]);      // 立刻销毁，触发GC
            this.EntityObjects.Remove(cha.entityId);
        }
    }

    /// <summary>
    /// 创建单个角色（玩家自己和其他玩家都用这个）
    /// </summary>
    /// <param name="character"></param>
    private void CreateCharacterObject(Character character)
    {
        // 只有角色不存在的时候才创建
        if (!EntityObjects.ContainsKey(character.entityId) || EntityObjects[character.entityId] == null)
        {
            // 使用 Resloader 资源加载器加载配置表资源
            Object obj = Resloader.Load<Object>(character.Define.Resource); 
            if(obj == null)
            {
                Debug.LogErrorFormat("[GameObjectManager] Character[{0}] Resource[{1}] not existed.",character.Define.TID, character.Define.Resource);
                return;
            }

            // Character 实例化
            GameObject go = (GameObject)Instantiate(obj, this.transform);   // 堆分配
            go.name = "Character_" + character.Info.Id + "_" + character.Info.Name;
            EntityObjects[character.entityId] = go;
 
            UIWorldElementManager.Instance.AddCharacterNameBar(go.transform, character);
        }

        // Character 初始化
        this.InitGameObject(EntityObjects[character.entityId], character);
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

    #region 怪物相关
    void OnMonsterEnter(Monster monster)
    {
        CreateMonsterObject(monster);
    }

    void OnMonsterLeave(Monster monster)
    {
        if (!EntityObjects.ContainsKey(monster.entityId)) return;
        if (EntityObjects[monster.entityId] != null)
        {
            Destroy(EntityObjects[monster.entityId]);
            EntityObjects.Remove(monster.entityId);
        }
    }

    /// <summary>
    /// 收到服务端的怪物状态同步消息，切换对应怪物的 AI 行为
    /// </summary>
    public void OnMonsterStateSync(MonsterStateSync sync)
    {
        // 用 MonsterEntityId 从 Characters 字典里找到对应的 GameObject
        if (!EntityObjects.ContainsKey(sync.MonsterEntityId))
        {
            Debug.LogWarningFormat("[GameObjectManager] OnMonsterStateSync: entityId:{0} 不存在",
                sync.MonsterEntityId);
            return;
        }

        GameObject monsterGo = EntityObjects[sync.MonsterEntityId];
        if (monsterGo == null) return;

        MonsterPatrolController patrol = monsterGo.GetComponent<MonsterPatrolController>();
        if (patrol == null) return;

        // 根据状态类型切换行为
        switch (sync.State)
        {
            case MonsterState.MonsterChase:
                // param 是目标玩家的 entityId
                patrol.StartChase(sync.Param);
                break;

            case MonsterState.MonsterReturn:
                patrol.StartReturn();
                break;

            case MonsterState.MonsterPatrol:
                patrol.StartPatrol();
                break;
        }
    }

    private void CreateMonsterObject(Monster monster)
    {
        if (!EntityObjects.ContainsKey(monster.entityId) || EntityObjects[monster.entityId] == null)
        {
            Object obj = Resloader.Load<Object>(monster.Define.Resource);
            if (obj == null)
            {
                Debug.LogErrorFormat("[GameObjectManager] Monster[{0}] Resource[{1}] 不存在.",
                    monster.Info.ConfigId, monster.Define.Resource);
                return;
            }

            GameObject go = (GameObject)Instantiate(obj, this.transform);
            go.name = "Monster_" + monster.Info.ConfigId + "_" + monster.Define.Name;
            EntityObjects[monster.entityId] = go;

            // 先不初始化巡逻，把怪物加进待初始化列表
            // 等场景加载完成后统一处理
            pendingPatrolInits.Add(monster);
        }

        GameObject monsterGo = EntityObjects[monster.entityId];

        // 第一步：先设置位置和朝向
        // NavMeshAgent 必须先在正确的位置上，才能正确计算路径
        monsterGo.transform.position = GameObjectTool.LogicToWorld(monster.position);
        monsterGo.transform.forward = GameObjectTool.LogicToWorld(monster.direction);
    }


    private void InitMonsterPatrol(GameObject monsterGo, Monster monster)
    {
        // 位置确定后，再注入巡逻数据并启动寻路
        // 顺序不能反，否则 SetDestination 在错误位置计算路径
        // 位置被覆盖后路径失效，怪物就站着不动
        SpawnPoint sp = FindSpawnPointById(monster.Info.SpawnPointId);
        MonsterPatrolController patrol = monsterGo.GetComponent<MonsterPatrolController>();

        Debug.LogFormat("[GameObjectManager] InitPatrol Monster:{0} SpawnPointId:{1} sp:{2} patrol:{3}",
            monster.Info.ConfigId,
            monster.Info.SpawnPointId,
            sp != null ? sp.ID.ToString() : "null",
            patrol != null ? "有" : "null");

        if (patrol != null && sp != null)
        {
            patrol.Init(sp.patrolPoints, sp.speed, sp.stoppingDistance, sp.viewRadius, sp.viewAngle);
        }
        else
        {
            if (patrol == null)
                Debug.LogWarningFormat("[GameObjectManager] Monster[{0}] 没有 MonsterPatrolController 组件", monster.Info.ConfigId);
            if (sp == null)
                Debug.LogWarningFormat("[GameObjectManager] SpawnPoint ID:{0} 未找到，怪物无法巡逻", monster.Info.SpawnPointId);
        }
    }

    /// <summary>
    /// 在场景里找到对应 ID 的 SpawnPoint
    /// </summary>
    SpawnPoint FindSpawnPointById(int spawnPointId)
    {
        SpawnPoint[] allSpawnPoints = FindObjectsOfType<SpawnPoint>();
        foreach (var sp in allSpawnPoints)
        {
            if (sp.ID == spawnPointId)
                return sp;
        }
        Debug.LogWarningFormat("[GameObjectManager] SpawnPoint ID:{0} 不存在", spawnPointId);
        return null;
    }

    // 场景加载完成时统一初始化所有待处理的怪物巡逻数据
    private void OnLevelLoaded()
    {
        Debug.Log("[GameObjectManager] OnLevelLoaded 开始初始化怪物巡逻");
        foreach (var monster in pendingPatrolInits)
        {
            if (!EntityObjects.ContainsKey(monster.entityId)) continue;
            GameObject monsterGo = EntityObjects[monster.entityId];
            if (monsterGo == null) continue;

            InitMonsterPatrol(monsterGo, monster);
        }
        pendingPatrolInits.Clear();
    }
    #endregion
}
