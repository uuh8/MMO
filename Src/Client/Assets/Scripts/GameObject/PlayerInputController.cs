using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Entities;
using SkillBridge.Message;
using Services;
using UnityEngine.AI;
using Managers;
using UnityEngine.EventSystems;


/*
 * PlayerInputController 的职责（本地玩家专用）：
 * 1) 读取输入：键盘/鼠标
 * 2) 驱动本地表现：Rigidbody 速度、角色朝向、动画状态（通过 Character 的 Move/Stop 等）
 * 3) 驱动网络同步：把 EntityEvent + EntityData 发给服务端（MapEntitySync） 
 */
public class PlayerInputController : MonoBehaviour
{
    public Rigidbody rb;
    CharacterState state;

    public Character character;
    public EntityController entityController; 

    [Header("Mouse Look")]
    public float mouseXSensitivity = 2.0f;
    public float mouseYSensitivity = 1.0f;

    [Header("Jump Feel")]
    public float jumpPower = 13.0f;
    public float fallMultiplier = 4.5f;        // 下降加速倍率，越大落地越快
    public float lowJumpMultiplier = 4.0f;   // 上升减速倍率，越大跳跃越矮

    [Header("Move Sync Throttle")]
    public float turnAngle = 10f;         // 方向变化超过多少度，补发一次快照同步
    public float syncInterval = 0.10f;    // 至少每隔多久补发一次快照同步（避免一直小抖动但永不发包）

    public int speed;

    private NavMeshAgent agent;
    private bool autoNav;               // 是否正在自动寻路

    private bool jumpRequested = false; // 是否跳跃
    private float jumpRequestTime = 0f;
    private const float jumpBufferTime = 0.15f; // 跳跃缓冲时间窗口

    public bool isGrounded = false;
    public float groundCheckDistance = 0.1f;    // 地面检测距离
    public LayerMask groundLayer;   // 在 Inspector 里设置地面所在的 Layer
    private bool wasGrounded = false;

    private float lastSyncTime = 0f;
    private Vector3 lastSyncDir = Vector3.zero;
    private EntityEvent lastMoveEvent = EntityEvent.Idle;

    // LateUpdate 相关
    // 用于计算 speed（世界坐标）
    private Vector3 lastPos;
    // 位置漂移纠偏阈值（逻辑坐标单位）
    //  WorldToLogic 是 *100，所以 100 ≈ 1米
    public int positionSnapThreshold = 100;
    // 因为“位置纠偏”触发快照同步(EntityEvent.None)的最小间隔（秒）
    // 防止持续漂移时每帧发包
    public float snapSyncInterval = 0.10f;
    // 上一次因为“位置纠偏”发送 None 的时间
    private float lastSnapSyncTime = 0f;


    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($"[{name}] PlayerInputController 缺少 Rigidbody");
            enabled = false; // 直接禁用，防止刷屏
        }
    }

    // Use this for initialization
    void Start()
    {
        state = CharacterState.Idle;
        if(agent == null)
        {
            agent = this.gameObject.AddComponent<NavMeshAgent>();
            agent.stoppingDistance = 0.3f;  // 停止距离（在到目标前0.3m处就结束自动寻路）
        }
    }

    /// <summary>
    /// 鼠标控制镜头（只改相机 yaw/pitch，不再用 A/D 旋转角色）
    /// </summary>
    private void Update()
    {
        if (character == null) return;
        if (MainPlayerCamera.Instance == null) return;

        // ---- 鼠标悬停 UI 时显示光标 ----
        bool isOverUI = EventSystem.current != null
                     && EventSystem.current.IsPointerOverGameObject();
        if (isOverUI != InputManager.Instance.IsOverUI)
        {
            InputManager.Instance.IsOverUI = isOverUI;
            InputManager.Instance.UpdateCursor();
        }

        // 1. 地面检测：放在所有 return 之前，保证每帧都执行
        RaycastHit hit;
        isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.05f, // 从脚底略微抬高
            Vector3.down,
            out hit,
            0.15f  // 检测距离，比胶囊底部到地面的距离略大一点
        );

        // 2. 跳跃检测：只有在地面才记录跳跃意图
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequested = true;
            jumpRequestTime = Time.time;
        }

        // 落地检测：从空中落回地面的那一帧
        if (isGrounded && !wasGrounded)
        {
            // 重新启用 NavMeshAgent
            if (agent != null && !agent.enabled)
                agent.enabled = true;
        }
        wasGrounded = isGrounded;


        // UI 快捷键：按键打开面板，光标由 UIWindow.OnEnable 自动管理
        if (Input.GetKeyDown(KeyCode.B))
            UIManager.Instance.Show<UIBag>();

        if (Input.GetKeyDown(KeyCode.C))
            UIManager.Instance.Show<UICharEquip>();

        if (Input.GetKeyDown(KeyCode.Q))
            UIManager.Instance.Show<UIQuestSystem>();

        if (Input.GetKeyDown(KeyCode.F))
            UIManager.Instance.Show<UIFriends>();

        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("[PlayerInputController] OnClickGuild called");
            GuildManager.Instance.ShowGuildUI();
        }

        if (Input.GetKeyDown(KeyCode.R))
            UIManager.Instance.Show<UIRide>();

        if (Input.GetKeyDown(KeyCode.E))
            NPCManager.Instance.OnInteractKeyPressed();

        // ESC：关闭最上层的 UI，如果没有 UI 就打开设置
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!UIManager.Instance.CloseTop())
                UIManager.Instance.Show<UISetting>();
        }
        // ── 战斗输入 ──
        HandleCombatInput();


        // Gameplay 模式：处理鼠标相机旋转
        // 只有在光标未显示时才旋转相机
        if (!InputManager.Instance.ShouldShowCursor)
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(mx) > 0.0001f || Mathf.Abs(my) > 0.0001f)
            {
                MainPlayerCamera.Instance.AddRotation(
                    mx * mouseXSensitivity,
                    -my * mouseYSensitivity
                );
            }
        }
    }


    private void OnDrawGizmos()
    {
        // 绿色=在地面，红色=在空中
        Gizmos.color = isGrounded ? Color.green : Color.red;
        // 画一个小球在角色脚底，直观看检测位置
        Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.05f, 0.1f);
    }
    /// <summary>
    /// FixedUpdate：处理“键盘移动”
    /// 为什么在 FixedUpdate？
    /// - 用 Rigidbody 推动角色速度，这是物理系统的一部分，放在 FixedUpdate 更稳定
    /// </summary>
    void FixedUpdate()
    {
        if (character == null) return;
        if (InputManager.Instance.IsInputMode) return;

        // 自动寻路优先
        if (autoNav)
        {
            NavMove();
            return;
        }

        float v = Input.GetAxis("Vertical");   // W/S
        float h = Input.GetAxis("Horizontal"); // A/D

        // 1) 取“真实 Camera”的 forward/right 作为移动参考
        //    直觉：W 应该让角色朝“屏幕远处/远离相机”走，而不是朝相机看过去的方向走
        Transform camT = MainPlayerCamera.Instance.camera.transform;

        // 1) 取相机 forward/right 作为“移动参考系”
        //    这一步是把控制从“角色自身坐标系”改为“镜头坐标系”
        Vector3 camForward = camT.forward;
        Vector3 camRight = camT.right;

        // 2) 手动清除 Y 分量，把向量投影到 XZ 水平面。相机有俯仰角，forward 会带有 Y 分量，不清除的话按 W 键角色会朝斜下方或斜上方移动。
        camForward.y = 0f;
        camRight.y = 0f;

        // sqrMagnitude：向量长度的平方，比 magnitude（需要开平方根）更高效。这里用来检测去掉 Y 分量后向量是否接近零向量
        if (camForward.sqrMagnitude < 1e-6f) camForward = Vector3.forward;
        if (camRight.sqrMagnitude < 1e-6f) camRight = Vector3.right;

        camForward.Normalize();
        camRight.Normalize();

        // 3) 合成移动方向：moveDir = 前后 + 左右走位
        Vector3 moveDir = camForward * v + camRight * h;
        // 斜向归一化：避免斜着走更快
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        bool isMoving = moveDir.sqrMagnitude > 0.0001f;

        if (isMoving)
        {
            // 4) 表现层朝向：移动时让角色面向移动方向
            this.transform.forward = moveDir;
            // 5) 把移动方向从世界坐标（float Vector3）转换为逻辑坐标（int Vector3Int），写入逻辑层
            //    EntityData.Direction 会被用于逻辑位移模拟（Entity.OnUpdate）
            //    如果不更新，它会按旧方向积分，导致本地逻辑与刚体位置不一致
            character.SetDirection(GameObjectTool.WorldToLogic(moveDir));

            // 6) 状态机逻辑：用 state 变量记录当前是 Idle 还是 Move只在状态发生变化时才发事件，避免每帧都发包和触发动画。
            // v < -0.1f 判断是否在后退（S键），用0.1而不是0是为了避免轴值的微小抖动被误判为后退。
            EntityEvent desiredMoveEvent = (v < -0.1f) ? EntityEvent.MoveBack : EntityEvent.MoveFwd;

            if (state != CharacterState.Move)
            {
                // 从 Idle -> Move：切状态 + 发一次移动事件
                state = CharacterState.Move;
                // 让 Character 的速度/状态进入移动（服务端/动画可能依赖这个）
                character.MoveForward();
                lastMoveEvent = desiredMoveEvent;
                SendEntityEvent(desiredMoveEvent);
            }
            else if (desiredMoveEvent != lastMoveEvent)
            {
                // 前进/后退切换时补发事件，让服务端/动画一致
                lastMoveEvent = desiredMoveEvent;
                SendEntityEvent(desiredMoveEvent);
            }

            // 7) 刚体速度推进
            this.rb.velocity = this.rb.velocity.y * Vector3.up
                             + moveDir * (this.character.speed + 9.81f) / 100f;

            // 空中减弱重力
            if (!isGrounded)
            {
                if (rb.velocity.y < 0)
                {
                    // 下降阶段：施加额外向下的力，让角色快速落地
                    // fallMultiplier 越大下降越快，建议 2~4
                    rb.AddForce(Physics.gravity * rb.mass * fallMultiplier);
                }
                else if (rb.velocity.y > 0)
                {
                    // 上升阶段：施加额外向下的力，让上升更短促
                    // lowJumpMultiplier 越大跳跃越矮越快，建议 1~2
                    rb.AddForce(Physics.gravity * rb.mass * lowJumpMultiplier);
                }
            }
            // 8) 节流快照同步（EntityEvent.None）：
            //    移动中不应该每个 FixedUpdate 都发包，否则网络压力和服务端压力都很大
            TrySendSnapshotIfNeeded(moveDir);
        }
        else
        {
            // 停止移动：切 Idle + 停止速度 + 发 Idle 事件
            if (state != CharacterState.Idle)
            {
                state = CharacterState.Idle;
                lastMoveEvent = EntityEvent.Idle;

                this.rb.velocity = Vector3.zero;
                this.character.Stop();
                this.SendEntityEvent(EntityEvent.Idle);
            }
        }

        // 跳跃处理：移出 isMoving 判断块，站着和跑着都能跳
        if (jumpRequested)
        {
            // 超过缓冲时间窗口，丢弃这次跳跃
            if (Time.time - jumpRequestTime > jumpBufferTime)
            {
                jumpRequested = false;
            }
            else
            {
                jumpRequested = false;

                // 跳跃前禁用 NavMeshAgent，让 Rigidbody 接管 Y 轴
                if (agent != null && agent.enabled)
                {
                    agent.enabled = false;
                }

                // 清除当前 Y 轴速度，防止速度叠加
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                // 施加向上的冲量，ForceMode.Impulse 是瞬时力，适合跳跃
                rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// 移动中的节流同步：
    /// 移动过程中不能每帧发包（FixedUpdate 50次/秒），但也不能完全不发，所以用两个条件触发快照：明显转向了（服务器需要知道新方向）或者时间到了（定时兜底，防止长时间不发包导致服务器位置偏差积累）。
    /// </summary>
    private void TrySendSnapshotIfNeeded(Vector3 moveDir)
    {
        if (lastSyncDir == Vector3.zero)
            lastSyncDir = moveDir;

        // Vector3.Angle(a, b)：计算两个向量之间的夹角，返回0到180之间的角度值（度数）。
        float angle = Vector3.Angle(lastSyncDir, moveDir);
        bool angleChanged = angle >= turnAngle; // 转向超过10度
        bool timeReached = (Time.time - lastSyncTime) >= syncInterval;  // 超过0.1秒

        if (angleChanged || timeReached)
        {
            lastSyncDir = moveDir;
            lastSyncTime = Time.time;
            // EntityEvent.None：不切动画状态，只同步 position/direction/speed 等实体快照
            SendEntityEvent(EntityEvent.None);
        }
    }

    /// <summary>
    /// 把最新刚体结果对齐模型 → 相机更新 → 渲染输出
    /// </summary>
    private void LateUpdate()
    {
        if (this.character == null) return;

        // ------------------------------------------------------------
        // 1) 计算速度（用于 UI/调试/可能的同步）
        //    用 Rigidbody 的真实位移计算，碰撞/挤压/坡度等情况下也可信
        // ------------------------------------------------------------
        Vector3 offset = this.rb.transform.position - lastPos;
        this.speed = (int)(offset.magnitude * 100f / Time.deltaTime);
        lastPos = this.rb.transform.position;

        // ------------------------------------------------------------
        // 2) 渲染层对齐物理层
        //    由于是用 rb 驱动移动，所以最终显示位置必须跟随 rb，否则会出现“模型和碰撞体分离”
        // ------------------------------------------------------------
        this.transform.position = this.rb.transform.position;

        // ------------------------------------------------------------
        // 3) 逻辑层位置纠偏（关键）
        //    rb.position 是“物理真实结果”，character.position 是“逻辑/网络层状态”。
        //    当两者差太大，说明逻辑层漂移了（常见于碰撞/外力/推挤/台阶/卡边修正）。
        //    纠偏策略：以 rb 为准，把逻辑位置拉回 rb，并用 None 做快照同步。
        //
        //    注意：这里只纠“位置”，不在 LateUpdate 纠“方向”。
        //    方向应该由 FixedUpdate 的输入决策统一管理，否则会与控制逻辑打架。
        // ------------------------------------------------------------
        Vector3Int rbLogicPos = GameObjectTool.WorldToLogic(this.rb.transform.position);
        Vector3Int delta = rbLogicPos - this.character.position;

        // 用 sqrMagnitude 避免 sqrt、也避免 float->int 隐式转换报错
        int threshold = positionSnapThreshold;
        int thresholdSqr = threshold * threshold;

        if (delta.sqrMagnitude > thresholdSqr)
        {
            // 让逻辑层承认物理现实
            this.character.SetPosition(rbLogicPos);

            // 节流：避免持续漂移时每帧发送 None 快照包
            if (Time.time - lastSnapSyncTime >= snapSyncInterval)
            {
                lastSnapSyncTime = Time.time;
                this.SendEntityEvent(EntityEvent.None);
            }
        }
    }


    public void SendEntityEvent(EntityEvent entityEvent, int param = 0)
    {
        // 本地先处理（比如本地动画/状态）
        if (entityController != null)
            entityController.OnEntityEvent(entityEvent, param);    // 动画

        // 同步给服务端：服务端再广播给其他客户端
        MapService.Instance.SendMapEntitySync(entityEvent, this.character.EntityData, param);
    }


    #region 自动寻路

    public void StartNav(Vector3 target)
    {
        StartCoroutine(BeginNav(target));
    }

    IEnumerator BeginNav(Vector3 target)
    {
        agent.SetDestination(target);   // 设置完这一刻，后台就自动调用寻路算法开始计算了
        yield return null;
        autoNav = true;

        // Move的动作
        if (state != CharacterState.Move)
        {
            state = CharacterState.Move;
            this.character.MoveForward();
            this.SendEntityEvent(EntityEvent.MoveFwd);
            agent.speed = this.character.speed / 100f;
        }
    }

    public void StopNav()
    {
        autoNav = false;
        agent.ResetPath();

        // 停止行走的动作，转为Idle
        if (state != CharacterState.Idle)
        {
            state = CharacterState.Idle;
            this.rb.velocity = Vector3.zero;
            this.character.Stop();
            this.SendEntityEvent(EntityEvent.Idle);
        }
        NavPathRenderer.Instance.SetPath(null, Vector3.zero);
    }

    /// <summary>
    /// “寻路中”的逻辑
    /// </summary>
    public void NavMove()
    {
        if (agent.pathPending || agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            return;  // 寻路还没完成，直接返回
        }
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            // target的路径不可到达，直接停止
            StopNav();
            return;
        }

        /// 如果能执行到下面逻辑说明寻路一定完成了
        if (Mathf.Abs(Input.GetAxis("Vertical")) > 0.1 || Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1)
        {
            // 玩家有主动输入，说明玩家想结束自动寻路
            StopNav();
            return;
        }

        // 自动寻路路径渲染
        NavPathRenderer.Instance.SetPath(agent.path, agent.destination);

        if (agent.isStopped || agent.remainingDistance < 0.3f)
        {
            StopNav();
            return;
        }
    }

    #endregion

    private void HandleCombatInput()
    {
        // UI 打开时 / 聊天框输入中，不响应战斗按键
        if (InputManager.Instance.IsInputMode) return;
        if (InputManager.Instance.IsOverUI) return;

        // 左键 - 普攻A（排除点在 UI 上的情况）
        if (Input.GetMouseButtonDown(0)
            && !EventSystem.current.IsPointerOverGameObject())
        {
            SendEntityEvent(EntityEvent.AtkA);
        }

        // 右键 - 普攻B
        if (Input.GetMouseButtonDown(1))
            SendEntityEvent(EntityEvent.AtkB);

        // 1 / 2 / 3 - 技能（E 和 R 已被 NPC交互 和 坐骑 占用）
        if (Input.GetKeyDown(KeyCode.J))
            SendEntityEvent(EntityEvent.SkillA);

        if (Input.GetKeyDown(KeyCode.K))
            SendEntityEvent(EntityEvent.SkillB);

        if (Input.GetKeyDown(KeyCode.L))
            SendEntityEvent(EntityEvent.SkillC);
    }
}
