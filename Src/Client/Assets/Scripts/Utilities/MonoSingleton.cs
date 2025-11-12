using UnityEngine;


public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // 控制该单例是否常驻（是否在场景切换时调用 DontDestroyOnLoad）
    public bool global = true;

    // 静态持有的实例引用（单例核心）
    static T instance;

    // 公共静态访问器，按需返回场景中已存在的实例
    public static T Instance
    {
        get
        {
            // 如果静态引用为空，尝试通过 FindObjectOfType 在当前场景中寻找
            if (instance == null)
            {
                instance =(T)FindObjectOfType<T>();
            }
            return instance;
        }

    }

    // Awake 在对象激活并在 Start 之前调用 —— 这里用 Awake 来确保生命周期控制优先执行
    void Awake()
    {
        // Debug.LogFormat("[MonoSingleton] {0}[{1}] Awake", typeof(T), this.GetInstanceID());

        // 如果设置为常驻（global），则执行常驻和重复实例检测逻辑
        if (global)
        {
            // 如果已有一个实例并且不是当前自己（说明这是重复创建的另一个 GameObject）
            if (instance != null && instance != this.gameObject.GetComponent<T>())
            {
                // 发现重复实例 -> 直接销毁当前的 GameObject（自毁，保留第一个实例）
                Destroy(this.gameObject);
                return; // 终止后续初始化
            }

            // 把当前 GameObject 标记为场景切换时不被销毁（使其成为常驻对象）
            DontDestroyOnLoad(this.gameObject);

            // 将静态引用指向当前对象的组件（确保 Instance 可用）
            instance = this.gameObject.GetComponent<T>();
        }
        this.OnStart();
    }

    // 子类覆写这个方法做初始化（勿直接覆写 Awake）
    protected virtual void OnStart()
    {

    }
}