using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Entities;
using SkillBridge.Message;
using Services;
using UnityEngine.AI;


/*
 * PlayerInputController 的职责（本地玩家专用）：
 * 1) 读取输入：键盘/鼠标
 * 2) 驱动本地表现：Rigidbody 速度、角色朝向、动画状态（通过 Character 的 Move/Stop 等）
 * 3) 驱动网络同步：把 EntityEvent + EntityData 发给服务端（MapEntitySync）
 *
 * 你要的改动核心：
 * - 鼠标：只控制相机 yaw/pitch（镜头旋转），不再用 A/D 旋转角色
 * - 键盘：W/S 前后，A/D 左右走位（Strafe），移动方向以“相机朝向”为参考
 */
public class PlayerInputController : MonoBehaviour
{
    public Rigidbody rb;
    CharacterState state;

    public Character character;
    public EntityController entityController;

    [Header("Mouse Look")]
    public float mouseXSensitivity = 3.0f;
    public float mouseYSensitivity = 2.0f;

    [Tooltip("避免UI操作时镜头乱转：勾上表示只有按住右键才允许旋转镜头。")]
    public bool rotateOnlyWhenRightMouseHeld = true;

    [Header("Move Sync Throttle")]
    public float turnAngle = 10f;         // 方向变化超过多少度，补发一次快照同步
    public float syncInterval = 0.10f;    // 至少每隔多久补发一次快照同步（避免一直小抖动但永不发包）

    public int speed;
    public bool onAir = false;

    private NavMeshAgent agent;
    private bool autoNav;                // 是否正在自动寻路

    private float lastSyncTime = 0f;
    private Vector3 lastSyncDir = Vector3.zero;
    private EntityEvent lastMoveEvent = EntityEvent.Idle;

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
        if (this.character == null)
        {
            DataManager.Instance.Load();

            NCharacterInfo cinfo = new NCharacterInfo();
            cinfo.Id = 1;
            cinfo.Name = "Test";
            cinfo.ConfigId = 1;
            cinfo.Entity = new NEntity();
            cinfo.Entity.Position = new NVector3();
            cinfo.Entity.Direction = new NVector3();
            //cinfo.Entity.Direction.X = 0;
            //cinfo.Entity.Direction.Y = 0;
            //cinfo.Entity.Direction.Z = 0;

            this.character = new Character(cinfo);

            if (entityController != null) entityController.entity = this.character;
        }

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

        // ===============================
        // 1. 聊天 / 输入模式优先
        // ===============================
        if (InputManager.Instance != null && InputManager.Instance.IsInputMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // ===============================
        // 2. Alt 键：临时 UI 模式
        // ===============================
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return; // UI 模式下不旋转镜头
        }

        // ===============================
        // 3. 正常 Gameplay 模式
        // ===============================
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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
    /// <summary>
    /// FixedUpdate：处理“键盘移动”
    /// 为什么在 FixedUpdate？
    /// - 用 Rigidbody 推动角色速度，这是物理系统的一部分，放在 FixedUpdate 更稳定
    /// </summary>
    void FixedUpdate()
    {
        if (character == null) return;

        // 自动寻路优先
        if (autoNav)
        {
            NavMove();
            return;
        }

        if (InputManager.Instance.IsInputMode) return;

        float v = Input.GetAxis("Vertical");   // W/S
        float h = Input.GetAxis("Horizontal"); // A/D

        // 1) 取“真实 Camera”的 forward/right 作为移动参考（不要用父物体 transform）
        //    直觉：W 应该让角色朝“屏幕远处/远离相机”走，而不是朝相机看过去的方向走
        Transform camT = (MainPlayerCamera.Instance != null && MainPlayerCamera.Instance.camera != null)
            ? MainPlayerCamera.Instance.camera.transform
            : null;

        // 1) 取相机 forward/right 作为“移动参考系”
        //    这一步是把控制从“角色自身坐标系”改为“镜头坐标系”
        //    - W 永远朝屏幕前方走
        //    - A/D 永远是屏幕左右走位
        Vector3 camForward = (camT != null) ? -camT.forward : Vector3.forward;
        Vector3 camRight = (camT != null) ? -camT.right : Vector3.right;

        // 2) 投影到地面（XZ）：我们不希望因为镜头俯仰导致 forward 带 y 分量，让角色“往天上走/钻地”
        camForward.y = 0f;
        camRight.y = 0f;

        if (camForward.sqrMagnitude < 1e-6f) camForward = Vector3.forward;
        if (camRight.sqrMagnitude < 1e-6f) camRight = Vector3.right;

        camForward.Normalize();
        camRight.Normalize();

        // 3) 合成移动方向：moveDir = 前后 + 左右走位
        Vector3 moveDir = camForward * v + camRight * h;

        // 斜向归一化：避免斜着走更快（经典小坑）
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        bool isMoving = moveDir.sqrMagnitude > 0.0001f;

        if (isMoving)
        {
            // 4) 表现层朝向：移动时让角色面向移动方向（大多数第三人称动作/MMO就是这么做）
            //    注意：镜头旋转不依赖 player.forward，所以 A/D 不会再带动镜头转
            this.transform.forward = moveDir;

            // 5) 逻辑/网络层 direction 也要更新：
            //    你项目里 EntityData.Direction 会被用于逻辑位移模拟（Entity.OnUpdate）
            //    如果不更新，它会按旧方向积分，导致本地逻辑与刚体位置不一致
            character.SetDirection(GameObjectTool.WorldToLogic(moveDir));

            // 6) 事件选择：
            //    - v<0 看作后退（MoveBack）
            //    - 其他情况都用 MoveFwd（包括纯 A/D 走位也播放跑步即可）
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

            // 7) 刚体速度推进：
            //    沿 moveDir 推进，速度用你项目原换算方式（/100f + 9.81f）保持手感一致
            this.rb.velocity = this.rb.velocity.y * Vector3.up
                             + moveDir * (this.character.speed + 9.81f) / 100f;

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

        // 跳跃：你原逻辑保留
        if (Input.GetButtonDown("Jump"))
        {
            this.SendEntityEvent(EntityEvent.Jump);
        }
    }

    /// <summary>
    /// 只做“快照同步”(EntityEvent.None)：
    /// - 方向变化超过 turnAngle：说明玩家明显转向了，需要同步
    /// - 或超过 syncInterval：即使方向一直小抖动，也要定时同步位置/方向，防止长时间不发包
    /// </summary>
    private void TrySendSnapshotIfNeeded(Vector3 moveDir)
    {
        if (lastSyncDir == Vector3.zero)
            lastSyncDir = moveDir;

        float angle = Vector3.Angle(lastSyncDir, moveDir);
        bool angleChanged = angle >= turnAngle;
        bool timeReached = (Time.time - lastSyncTime) >= syncInterval;

        if (angleChanged || timeReached)
        {
            lastSyncDir = moveDir;
            lastSyncTime = Time.time;

            // EntityEvent.None：不切动画状态，只同步 position/direction/speed 等实体快照
            SendEntityEvent(EntityEvent.None);
        }
    }

    // ===== LateUpdate相关的类字段区 =====
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


}
