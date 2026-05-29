using SkillBridge.Message;
using UnityEngine;
using Entities;
using Managers;

/*负责把逻辑层的数据翻译成玩家看得见的东西。*/
public class EntityController : MonoBehaviour, IEntityNotify
{
    public Animator anim;
    public Rigidbody rb;

    public Entity entity;

    public Vector3 position;
    public Vector3 direction;
    public float speed;

    public bool isPlayer = false;

    public RideController rideController;
    public int currentRide = 0;
    public Transform rideBone;

    // Use this for initialization
    void Start () {
        // 全局关闭 Root Motion，所有动画的位移都由物理层负责
        anim.applyRootMotion = false;

        if (entity != null)
        {
            // 注册事件的接收者.
            // 注意这儿 this 是 EntityController 自己,但被当作 IEntityNotify 接口类型传入，EntityManager 只看到接口，看不到 EntityController，以此利用面向接口编程，实现逻辑层和表现层的解耦
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

        // 第一步：逻辑层外推    
        // 固定时间步调用 OnUpdate(delta) 做“逻辑积分”（pos += dir * speed * delta / SCALE），并把结果回写到 NEntity，保证本地逻辑与可发往网络的数据一致
        this.entity.OnUpdate(Time.fixedDeltaTime);

        // 第二步：逻辑坐标→世界坐标→驱动GameObject
        // 不是本地玩家，把逻辑投影到可视层（只对其他玩家执行，本地玩家的位置由 Rigidbody 物理驱动，不走这里）
        if (!this.isPlayer)
        {
            this.UpdateTransform();
        }
    }

    /// <summary>
    /// 把逻辑坐标映射到 Unity 的可视 Transform
    /// </summary>
    void UpdateTransform()
    {
        // 从逻辑坐标转换到世界坐标
        this.position = GameObjectTool.LogicToWorld(entity.position);   // 整数→浮点
        this.direction = GameObjectTool.LogicToWorld(entity.direction);

        this.rb.MovePosition(this.position);        // 更新物理位置
        this.transform.forward = this.direction;    // 更新模型朝向
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

#region IEntityNotify 接口实现

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
    /// 实体状态改变
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
                break;
            case EntityEvent.Ride:
                this.Ride(param);
                break;
            case EntityEvent.AtkA:
                anim.SetTrigger("AttackA");
                break;
            case EntityEvent.AtkB:
                anim.SetTrigger("AttackB");
                break;
            case EntityEvent.SkillA:
                anim.SetTrigger("SkillA");
                break;
            case EntityEvent.SkillB:
                anim.SetTrigger("SkillB");
                break;
            case EntityEvent.SkillC:
                anim.SetTrigger("SkillC");
                break;
        }

        if (this.rideController != null)
            this.rideController.OnEntityEvent(entityEvent, param);  // 角色做了动作坐骑也要跟着做动作
    }

    /// <summary>
    /// 实体数据改变
    /// </summary>
    /// <param name="entity"></param>
    /// <exception cref="System.NotImplementedException"></exception>
    public void OnEntityChanged(Entity entity)
    {
        // Debug.LogFormat("[EntityController] OnEntityChanged :ID:{0} POS:{1} DIR:{2} SPD:{3} ", entity.entityId, entity.position, entity.direction, entity.speed);
    }

#endregion

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
        // 全部用世界坐标，让 rideBone 对齐到 mountPoint
        this.anim.transform.position = position + (this.anim.transform.position - this.rideBone.position);
    }



}
