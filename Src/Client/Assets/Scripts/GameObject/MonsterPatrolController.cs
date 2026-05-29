using Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterPatrolController : MonoBehaviour
{
    // ── 运行时数据（由外部 Init 注入，不在 Inspector 里配置）────────
    private List<Vector3> patrolPoints;
    private float speed;
    private float stoppingDistance;
    // 视野参数（注入后供视野检测逻辑使用）
    private float viewRadius;
    private float viewAngle;

    // ── 巡逻状态 ──────────────────────────────────────────────────
    private NavMeshAgent agent;
    public int currentIndex = 0;    // 当前目标路径点的索引
    public bool isForward = true;   // true = 向终点走，false = 向起点走
    public int PatrolPointCount => patrolPoints != null ? patrolPoints.Count : 0;
    // 是否已经初始化 
    private bool initialized = false;

    // ── AI 状态 ───────────────────────────────────────────────────
    // 当前处于哪个状态
    private enum AIState { Patrol, Chase, Return }
    private AIState currentState = AIState.Patrol;

    // 追击目标的 Entity 对象，每帧从它身上读取最新位置
    private Entities.Entity chaseTarget = null;

    /// <summary>
    /// 由 GameObjectManager 在实例化怪物后调用
    /// 把 SpawnPoint 上配置的数据注入进来
    /// </summary>
    public void Init(List<Vector3> points, float spd, float stopDist, float viewR, float viewA)
    {
        patrolPoints        = points;
        speed               = spd;
        stoppingDistance    = stopDist;
        viewRadius          = viewR;
        viewAngle           = viewA;

        // 路径点不足两个，巡逻没有意义
        if (patrolPoints == null || patrolPoints.Count < 2)
        {
            Debug.LogWarning("[MonsterPatrolController] 路径点不足两个，无法巡逻");
            return;
        }

        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;

        initialized = true;

        // 出发前往第一个路径点
        SetDestination(currentIndex);
    }

    void Update()
    {
        // 没有初始化完成，不执行任何逻辑
        if (!initialized) return;

        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Return:
                // 返回状态由 NavMeshAgent 自动走到目标点
                // 走到目标点后什么都不做，等服务端发来 MonsterPatrol 消息再切回巡逻
                break;
        }
    }

    // ── 巡逻状态 Update ───────────────────────────────────────────
    void UpdatePatrol()
    {
        // 判断是否到达当前目标路径点
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            MoveToNextPoint();
        }
    }

    // ── 追击状态 Update ───────────────────────────────────────────
    void UpdateChase()
    {
        // 追击目标不存在，停止追击
        if (chaseTarget == null) return;

        // 每帧把 chaseTarget 的最新逻辑坐标转成世界坐标，设为 Agent 目标
        // 因为玩家在移动，目标位置每帧都在变，必须每帧更新
        Vector3 targetWorldPos = GameObjectTool.LogicToWorld(chaseTarget.position);
        agent.SetDestination(targetWorldPos);
    }

    /// <summary>
    /// 计算下一个目标点并前往
    /// </summary>
    void MoveToNextPoint()
    {
        if (isForward)
        {
            // 当前向终点方向走
            if (currentIndex < patrolPoints.Count - 1)
            {
                // 还没到终点，继续向前
                currentIndex++;
            }
            else
            {
                // 已经到达终点，掉头
                isForward = false;
                currentIndex--;
            }
        }
        else
        {
            // 当前向起点方向走
            if (currentIndex > 0)
            {
                // 还没到起点，继续向后
                currentIndex--;
            }
            else
            {
                // 已经到达起点，掉头
                isForward = true;
                currentIndex++;
            }
        }

        SetDestination(currentIndex);
    }

    /// <summary>
    /// 设置 NavMeshAgent 的目标点
    /// </summary>
    void SetDestination(int index)
    {
        agent.SetDestination(patrolPoints[index]);
    }

    // ── 三个状态切换入口（由 GameObjectManager 调用）─────────────

    /// <summary>
    /// 切换到追击状态
    /// targetEntityId：目标玩家的 entityId，用于每帧获取最新位置
    /// </summary>
    public void StartChase(int targetEntityId)
    {
        // 从 EntityManager 里找到目标玩家的 Entity 对象
        // 之后每帧从这个对象上读取最新坐标
        chaseTarget = EntityManager.Instance.GetEntity(targetEntityId);

        if (chaseTarget == null)
        {
            Debug.LogWarningFormat("[MonsterPatrolController] 追击目标 entityId:{0} 不存在",
                targetEntityId);
            return;
        }

        currentState = AIState.Chase;
        Debug.LogFormat("[MonsterPatrolController] 进入追击状态，目标 entityId:{0}", targetEntityId);
    }

    /// <summary>
    /// 切换到返回状态
    /// 找到最近的路径点，让 NavMeshAgent 走回去
    /// </summary>
    public void StartReturn()
    {
        currentState = AIState.Return;
        chaseTarget = null;

        // 找到距离当前位置最近的路径点作为返回目标
        Vector3 nearest = FindNearestPatrolPoint();
        agent.SetDestination(nearest);

        Debug.Log("[MonsterPatrolController] 进入返回状态");
    }

    /// <summary>
    /// 切换到巡逻状态
    /// 从最近的路径点开始重新巡逻
    /// </summary>
    public void StartPatrol()
    {
        currentState = AIState.Patrol;
        chaseTarget = null;

        // 找到最近的路径点，从这里开始巡逻而不是强制跳回 index=0
        // 避免怪物突然瞬移到起点
        currentIndex = FindNearestPatrolIndex();
        SetDestination(currentIndex);

        Debug.Log("[MonsterPatrolController] 进入巡逻状态");
    }

    // ── 辅助方法 ──────────────────────────────────────────────────
    /// <summary>
    /// 找到距离当前位置最近的路径点坐标
    /// </summary>
    Vector3 FindNearestPatrolPoint()
    {
        return patrolPoints[FindNearestPatrolIndex()];
    }

    /// <summary>
    /// 找到距离当前位置最近的路径点索引
    /// </summary>
    int FindNearestPatrolIndex()
    {
        int nearestIndex = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < patrolPoints.Count; i++)
        {
            float dist = Vector3.Distance(transform.position, patrolPoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                nearestIndex = i;
            }
        }
        return nearestIndex;
    }
}