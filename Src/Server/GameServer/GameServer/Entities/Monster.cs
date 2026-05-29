using GameServer.Core;
using GameServer.Managers;
using Common.Data;
using SkillBridge.Message;
using Common;
using System.Collections.Generic;

namespace GameServer.Entities
{
    // 怪物 AI 状态
    enum MonsterAIState
    {
        Patrol, // 巡逻
        Chase,  // 追击
        Return  // 返回
    }

    class Monster : CharacterBase
    {
        public MonsterDefine MonsterDefine { get; private set; }
        // 怪物专属的网络信息，独立于 CharacterBase.Info
        public NMonsterInfo MonsterInfo { get; private set; }
        // 缓存这只怪物对应的刷怪点定义，里面有 ViewRadius 和 ViewAngle
        private SpawnPointDefine spawnPointDefine;

        // ── 状态机 ────────────────────────────────────────────────
        // 当前 AI 状态，初始为巡逻
        private MonsterAIState currentState = MonsterAIState.Patrol;

        // 追击目标，切换到追击状态时记录
        private Character chaseTarget = null;

        // 持续多少帧没有发现玩家才切换到返回状态
        // 避免玩家短暂离开视野就立刻停止追击，让追击行为更自然
        private int lostTargetTimer = 0;
        private const int LOST_TARGET_THRESHOLD = 100; // 100 帧约 3 秒

        public Monster(int tid, int level, Vector3Int pos, Vector3Int dir)
            : base(CharacterType.Monster, tid, level, pos, dir)
        {
            // CharacterBase 构造完成后，再单独加载怪物自己的配置
            // 覆盖掉 CharacterBase 里设置的 Define（CharacterBase.Define 对怪物没意义）
            if (DataManager.Instance.Monsters.ContainsKey(tid))
            {
                MonsterDefine = DataManager.Instance.Monsters[tid];
                // 用怪物配置表里的名字覆盖 CharacterBase 里设置的名字
                this.Info.Name = MonsterDefine.Name;
            }
            else
            {
                Log.ErrorFormat("[Monster] MonsterDefine TID:{0} 不存在", tid);
            }

            // 初始化怪物专属网络信息
            // 从 CharacterBase.Info 里把公共数据同步过来
            MonsterInfo = new NMonsterInfo
            {
                ConfigId = tid,
                Level = level,
            };
        }

        /// <summary>
        /// 初始化刷怪点定义，由 MonsterManager.Create() 调用
        /// SpawnPointId 确定之后才能找到对应的刷怪点配置
        /// </summary>
        public void InitSpawnPoint(int spawnPointId)
        {
            // 从 DataManager 里找到这只怪物对应的刷怪点定义
            // SpawnPoints[mapId][spawnPointId]
            if (DataManager.Instance.SpawnPoints.ContainsKey(MonsterInfo.MapId) &&
                DataManager.Instance.SpawnPoints[MonsterInfo.MapId].ContainsKey(spawnPointId))
            {
                spawnPointDefine = DataManager.Instance.SpawnPoints[MonsterInfo.MapId][spawnPointId];
            }
            else
            {
                Log.ErrorFormat("[Monster] SpawnPointDefine MapId:{0} SpawnPointId:{1} 不存在",
                    MonsterInfo.MapId, spawnPointId);
            }
        }
        
        /// <summary>
        /// 视野检测：传入地图上所有玩家，遍历每一个玩家，判断这只怪物能不能看到他。找到第一个能看到的玩家就返回，都看不到返回 null。
        /// 由 MonsterManager.Update() 每帧调用
        /// </summary>
        /// <param name="characters">地图上所有玩家</param>
        /// <returns>第一个被发现的玩家，没有发现返回 null</returns>
        public Character CheckVision(IEnumerable<Character> characters)
        {
            // 没有刷怪点配置，无法获取视野参数
            if (spawnPointDefine == null) return null;

            // viewRadius 是世界单位（米），逻辑坐标是 ×100 的整数
            // 所以距离阈值要乘以 100
            // 例如：视野半径 5 米 → 逻辑距离阈值 500
            float radiusLogic = spawnPointDefine.ViewRadius * 100f;

            foreach (var character in characters)
            {
                // ── 条件一：距离检测 ──────────────────────────────
                // 为什么距离检测放在最前面？
                // 短路判断。距离检测是纯加减乘运算，成本最低。大多数玩家都在怪物视野范围之外，被距离条件过滤掉之后就不需要再算角度了，节省计算量。

                // 服务端用 Vector3Int，需要转成 float 做数学运算
                float dx = this.Position.x - character.Position.x;
                float dy = this.Position.y - character.Position.y;
                float dz = this.Position.z - character.Position.z;
                float distSqr = dx * dx + dy * dy + dz * dz;

                // 用距离的平方比较，避免开方运算（性能优化）
                // radiusLogic² 作为阈值
                if (distSqr > radiusLogic * radiusLogic)
                    continue; // 距离太远，跳过

                // ── 条件二：角度检测 ──────────────────────────────
                // 从怪物指向玩家的方向向量
                float dirX = character.Position.x - this.Position.x;
                float dirZ = character.Position.z - this.Position.z;

                // 怪物当前朝向（逻辑坐标里的方向向量）
                float forwardX = this.Direction.x;
                float forwardZ = this.Direction.z;

                // 用点积公式计算夹角的余弦值
                // cos(angle) = (A·B) / (|A| × |B|)

                // A·B
                float dotProduct = dirX * forwardX + dirZ * forwardZ;
                // |A|
                float lenDir = (float)System.Math.Sqrt(dirX * dirX + dirZ * dirZ);
                // |B|
                float lenForward = (float)System.Math.Sqrt(forwardX * forwardX + forwardZ * forwardZ);

                // 避免除以零（怪物和玩家重叠时）
                if (lenDir < 0.001f || lenForward < 0.001f)
                    continue;

                // (A·B) / (|A| × |B|)
                float cosAngle = dotProduct / (lenDir * lenForward);

                // viewAngle 是总视野角度，夹角要和半角比较
                // cos 函数单调递减：角度越大，cos 值越小
                // 夹角 < viewAngle/2 等价于 cos(夹角) > cos(viewAngle/2)
                float halfAngleRad = spawnPointDefine.ViewAngle * 0.5f
                                     * (float)System.Math.PI / 180f;
                float cosHalfAngle = (float)System.Math.Cos(halfAngleRad);

                if (cosAngle < cosHalfAngle)
                    continue; // 角度超出视野范围，跳过

                // 两个条件都满足，发现这个玩家
                Log.InfoFormat("[Monster] 发现玩家 monsterId:{0} characterId:{1}",
                    this.entityId, character.Id);
                return character;
            }

            return null; // 没有发现任何玩家
        }

        #region 怪物AI状态机
        /// <summary>
        /// 状态机每帧更新，由 MonsterManager.Update() 调用
        /// 返回需要广播给客户端的状态同步消息，没有变化返回 null
        /// </summary>
        public MonsterStateSync UpdateAI(IEnumerable<Character> characters)
        {
            switch (currentState)
            {
                case MonsterAIState.Patrol:
                    return UpdatePatrol(characters);
                case MonsterAIState.Chase:
                    return UpdateChase(characters);
                case MonsterAIState.Return:
                    return UpdateReturn();
                default:
                    return null;
            }
        }
        /// <summary>
        /// 巡逻状态更新
        /// 每帧做视野检测，发现玩家则切换到追击
        /// </summary>
        private MonsterStateSync UpdatePatrol(IEnumerable<Character> characters)
        {
            Character found = CheckVision(characters);
            if (found != null)
            {
                // 发现玩家，切换到追击状态
                return EnterChase(found);
            }
            return null; // 没有发现玩家，继续巡逻，不需要广播
        }
        /// <summary>
        /// 追击状态更新
        /// 持续检测目标是否还在视野内，丢失目标超过阈值则切换到返回
        /// </summary>
        private MonsterStateSync UpdateChase(IEnumerable<Character> characters)
        {
            Character found = CheckVision(characters);

            if (found != null)
            {
                // 还能看到玩家
                lostTargetTimer = 0;
                chaseTarget = found;

                // 如果追击目标发生了变化（比如原目标离开，另一个玩家进入视野）
                // 重新发送追击消息更新客户端的目标
                if (found.entityId != chaseTarget?.entityId)
                {
                    return BuildStateSync(MonsterState.MonsterChase, found.entityId);
                }

                return null; // 目标没变，不需要重复广播
            }
            else
            {
                // 看不到玩家了，开始计时
                lostTargetTimer++;

                if (lostTargetTimer >= LOST_TARGET_THRESHOLD)
                {
                    // 超过阈值，切换到返回状态
                    return EnterReturn();
                }
                return null;
            }
        }
        /// <summary>
        /// 返回状态更新
        /// 服务端只负责发出返回信号，具体"走回去"由客户端 NavMeshAgent 执行
        /// 服务端没有 NavMesh，无法判断是否已经回到路径点，所以返回状态固定持续一段时间后切回巡逻
        /// </summary>
        private int returnTimer = 0;
        private const int RETURN_DURATION = 300; // 300 帧约 10 秒，足够走回路径点
        private MonsterStateSync UpdateReturn()
        {
            returnTimer++;
            if (returnTimer >= RETURN_DURATION)
            {
                returnTimer = 0;
                return EnterPatrol();
            }
            return null;
        }

        // ── 状态切换函数 ──────────────────────────────────────────
        private MonsterStateSync EnterChase(Character target)
        {
            currentState = MonsterAIState.Chase;
            chaseTarget = target;
            lostTargetTimer = 0;

            Log.InfoFormat("[Monster] entityId:{0} 进入追击状态，目标:{1}",
                this.entityId, target.entityId);

            return BuildStateSync(MonsterState.MonsterChase, target.entityId);
        }

        private MonsterStateSync EnterReturn()
        {
            currentState = MonsterAIState.Return;
            chaseTarget = null;
            lostTargetTimer = 0;
            returnTimer = 0;

            Log.InfoFormat("[Monster] entityId:{0} 进入返回状态", this.entityId);

            return BuildStateSync(MonsterState.MonsterReturn, 0);
        }

        private MonsterStateSync EnterPatrol()
        {
            currentState = MonsterAIState.Patrol;

            Log.InfoFormat("[Monster] entityId:{0} 进入巡逻状态", this.entityId);

            return BuildStateSync(MonsterState.MonsterPatrol, 0);
        }

        /// <summary>
        /// 构建一条状态同步消息
        /// </summary>
        private MonsterStateSync BuildStateSync(MonsterState state, int param)
        {
            return new MonsterStateSync
            {
                MonsterEntityId = this.entityId,
                State = state,
                Param = param,
                // 同时把怪物当前位置带上，客户端用来校正起始位置
                Entity = this.EntityData
            };
        }
        #endregion
    }
}