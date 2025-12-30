using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Entities;
using SkillBridge.Message;
using Services;

/*该控制器用于接收用户的输入*/
public class PlayerInputController : MonoBehaviour
{
    public Rigidbody rb;
    CharacterState state;

    public Character character;
    public EntityController entityController;

    public float rotateSpeed = 2.0f;
    public float turnAngle = 10;
    public int speed;
    public bool onAir = false;

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
            cinfo.Entity.Direction.X = 0;
            cinfo.Entity.Direction.Y = 0;
            cinfo.Entity.Direction.Z = 0;

            this.character = new Character(cinfo);

            if (entityController != null) entityController.entity = this.character;
        }
    }

    /// <summary>
    /// 刚体计算物理 → 位移改变
    /// </summary>
    void FixedUpdate()
    {
        if (character == null)
            return;

        // 如果在输入模式（聊天模式），不执行移动
        if (InputManager.Instance.IsInputMode) return;

        float v = Input.GetAxis("Vertical");
        if (v > 0.01)   // 向前移动（0.01为浮点误差）
        {
            if (state != CharacterState.Move)
            {
                state = CharacterState.Move;
                // 这里的 MoveForward() 更新的是 “逻辑层” 变量（速度标记、事件派发）
                this.character.MoveForward();   
                // 通知 `EntityController` 播放“跑步”动画
                this.SendEntityEvent(EntityEvent.MoveFwd);
            }
            // 设置刚体速度。这里驱动的是 “表现层” 物理体，在屏幕上移动
            this.rb.velocity = this.rb.velocity.y * Vector3.up + GameObjectTool.LogicToWorld(character.direction) * (this.character.speed + 9.81f) / 100f;
        }
        else if (v < -0.01) // 向后移动
        {
            if (state != CharacterState.Move)
            {
                state = CharacterState.Move;
                this.character.MoveBack();  // 更新逻辑层（输入层到逻辑层）
                this.SendEntityEvent(EntityEvent.MoveBack);
            }
            this.rb.velocity = this.rb.velocity.y * Vector3.up + GameObjectTool.LogicToWorld(character.direction) * (this.character.speed + 9.81f) / 100f;
        }
        else    // 停止（待机）
        {
            if (state != CharacterState.Idle)
            {
                state = CharacterState.Idle;
                this.rb.velocity = Vector3.zero;
                this.character.Stop();
                this.SendEntityEvent(EntityEvent.Idle);
            }
        }
        // 跳跃
        if (Input.GetButtonDown("Jump"))
        {
            this.SendEntityEvent(EntityEvent.Jump);
        }
        // 左右（处理旋转）
        float h = Input.GetAxis("Horizontal");
        if (h < -0.1 || h > 0.1)
        {
            this.transform.Rotate(0, h * rotateSpeed, 0);
            Vector3 dir = GameObjectTool.LogicToWorld(character.direction);
            Quaternion rot = new Quaternion();
            rot.SetFromToRotation(dir, this.transform.forward);

            if (rot.eulerAngles.y > this.turnAngle && rot.eulerAngles.y < (360 - this.turnAngle))
            {
                character.SetDirection(GameObjectTool.WorldToLogic(this.transform.forward));
                rb.transform.forward = this.transform.forward;
                this.SendEntityEvent(EntityEvent.None);
            }

        }
        //Debug.LogFormat("velocity {0}", this.rb.velocity.magnitude);
    }

    Vector3 lastPos;        // 记录上一帧的位置
    float lastSync = 0;     // 记录上次同步的时间
    /// <summary>
    /// 把最新刚体结果对齐模型 → 相机更新 → 渲染输出
    /// </summary>
    private void LateUpdate()
    {
        if (this.character == null) return;

        // 计算本地视觉速度：通过刚体位移差 offset 算出本帧的实际速度，用于 UI 显示或动画驱动
        Vector3 offset = this.rb.transform.position - lastPos;
        this.speed = (int)(offset.magnitude * 100f / Time.deltaTime);
        //Debug.LogFormat("LateUpdate velocity {0} : {1}", this.rb.velocity.magnitude, this.speed);
        this.lastPos = this.rb.transform.position;

        // 同步逻辑位置：如果刚体的位置与逻辑层的 character.position 差距过大（>50 单位），就认为逻辑层“落后”，把刚体位置回写回去；
        if ((GameObjectTool.WorldToLogic(this.rb.transform.position) - this.character.position).magnitude > 100)
        {
            // 逻辑层在 `LateUpdate()`同步阶段读取当前刚体的世界坐标回写逻辑坐标
            this.character.SetPosition(GameObjectTool.WorldToLogic(this.rb.transform.position));
            this.SendEntityEvent(EntityEvent.None);
        }

        // 把 transform.position 对齐 rb.position，确保渲染与物理一致
        // 这一步不是同步逻辑层，而是同步渲染层。
        this.transform.position = this.rb.transform.position;
    }

    public void SendEntityEvent(EntityEvent entityEvent, int param = 0)
    {
        if (entityController != null)
            entityController.OnEntityEvent(entityEvent, param);    // 动画
        
        // 同步
        MapService.Instance.SendMapEntitySync(entityEvent, this.character.EntityData, param);
    }
}
