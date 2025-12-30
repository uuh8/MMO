using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Entities;
using Managers;

/*所有游戏对象都要绑这个脚本，负责把逻辑实体映射为可视 Transform，也负责接收输入层/网络层发来的“实体事件”切动画*/
public class EntityController : MonoBehaviour, IEntityNotify
{
    public Animator anim;
    public Rigidbody rb;
    private AnimatorStateInfo currentBaseState;

    public Entity entity;

    public Vector3 position;
    public Vector3 direction;
    Quaternion rotation;
    public Vector3 lastPosition;
    Quaternion lastRotation;

    public float speed;
    public float animSpeed = 1.5f;
    public float jumpPower = 3.0f;

    public bool isPlayer = false;

    public RideController rideController;
    public int currentRide = 0;
    public Transform rideBone;  // 设定一个位置和坐骑绑定(一般是臀部）

    // Use this for initialization
    void Start () {
        if (entity != null)
        {
            // 注册事件的接收者
            EntityManager.Instance.RegisterEntityChangeNotify(entity.entityId, this);
            this.UpdateTransform();
        }

        if (!this.isPlayer)
            rb.useGravity = false;
    }

    void FixedUpdate()
    {
        if (this.entity == null)
            return;

        // 固定时间步调用 OnUpdate(delta) 做“逻辑积分”（pos += dir * speed * delta / SCALE），并把结果回写到 NEntity，保证本地逻辑与可发往网络的数据一致
        this.entity.OnUpdate(Time.fixedDeltaTime);

        // 不是本地玩家，把逻辑投影到可视层
        // 本地玩家的可视层由 PlayerInputController 的 Rigidbody 驱动，EntityController 则避免覆盖本地刚体
        if (!this.isPlayer)
        {
            this.UpdateTransform();
        }
    }

    void OnDestroy()
    {
        if (entity != null)
            Debug.LogFormat("[EntityController] {0} OnDestroy :ID:{1} POS:{2} DIR:{3} SPD:{4} ", this.name, entity.entityId, entity.position, entity.direction, entity.speed);

        if (UIWorldElementManager.Instance != null)
        {
            UIWorldElementManager.Instance.RemoveCharacterNameBar(this.transform);
        }
    }

    /// <summary>
    /// 移除实体
    /// </summary>
    public void OnEntityRemoved()
    {
        if(UIWorldElementManager.Instance != null)
        {
            // 血条删掉
            UIWorldElementManager.Instance.RemoveCharacterNameBar(this.transform);
        }
        // 自己删掉
        Destroy(this.gameObject);
    }

    /// <summary>
    /// 逻辑到世界的映射（把逻辑层“落地”到可视层）
    /// </summary>
    void UpdateTransform()
    {
        // 从逻辑坐标转换到世界坐标
        this.position = GameObjectTool.LogicToWorld(entity.position);
        this.direction = GameObjectTool.LogicToWorld(entity.direction);

        this.rb.MovePosition(this.position);
        this.transform.forward = this.direction;
        this.lastPosition = this.position;
        this.lastRotation = this.rotation;
    }

    /// <summary>
    /// 状态改变
    /// </summary>
    /// <param name="entityEvent"></param>
    public void OnEntityEvent(EntityEvent entityEvent, int param)
    {
        switch(entityEvent)
        {
            case EntityEvent.Idle:
                anim.SetBool("Move", false);
                anim.SetTrigger("Idle");
                break;
            case EntityEvent.MoveFwd:
                anim.SetBool("Move", true);
                break;
            case EntityEvent.MoveBack:
                anim.SetBool("Move", true);
                break;
            case EntityEvent.Jump:
                anim.SetTrigger("Jump");
                break;
            case EntityEvent.Ride:
                this.Ride(param);
                break;
        }
        if (this.rideController != null)
            this.rideController.OnEntityEvent(entityEvent, param);  // 角色做了动作坐骑也要跟着做动作
    }

    public void Ride(int rideId)
    {
        if (currentRide == rideId) return;
        currentRide = rideId;
        if(rideId > 0)
        {
            // 上坐骑
            this.rideController = GameObjectManager.Instance.LoadRide(rideId, this.transform);
        }
        else
        {
            // 下坐骑
            Destroy(this.rideController.gameObject);
            this.rideController = null;
        }

        if(this.rideController == null)
        {
            this.anim.transform.localPosition = Vector3.zero;
            this.anim.SetLayerWeight(1, 0);
        }
        else
        {
            this.rideController.SetRider(this);
            this.anim.SetLayerWeight(1, 1);
        }
    }
    /// <summary>
    /// 设置角色乘坐坐骑的状态
    /// </summary>
    /// <param name="position"></param>
    public void SetRidePosition(Vector3 position)
    {
        this.anim.transform.position = position + (this.anim.transform.position - this.rideBone.position);
    }

    /// <summary>
    /// 数据改变
    /// </summary>
    /// <param name="entity"></param>
    /// <exception cref="System.NotImplementedException"></exception>
    public void OnEntityChanged(Entity entity)
    {
        // Debug.LogFormat("[EntityController] OnEntityChanged :ID:{0} POS:{1} DIR:{2} SPD:{3} ", entity.entityId, entity.position, entity.direction, entity.speed);
    }


}
