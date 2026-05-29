using UnityEditor;  // EditorWindow、EditorGUILayout 等编辑器专用 API 都在这个命名空间里
using UnityEngine; // GameObject、Debug 等运行时 API 在这里

// 继承 EditorWindow，告诉 Unity：这个类是一个编辑器窗口，帮我管理它的生命周期
public class EntityInspectorWindow : EditorWindow
{
    // ── 第一部分：开门 ──────────────────────────────────────────

    [MenuItem("GameObject/工具/Entity 调试窗口")]
    static void Open()
    {
        // GetWindow 是 EditorWindow 提供的静态方法
        // 作用：如果这个窗口已经开着，就把它聚焦到前台
        //       如果还没开，就新建一个并显示出来
        // 第二个参数 "Entity 调试" 是窗口标题栏显示的文字
        // 注意：这是 static 方法，所以不需要窗口实例就能调用
        GetWindow<EntityInspectorWindow>("Entity 调试");
    }

    // ── 第二部分：监听 Hierarchy 选中变化 ───────────────────────

    // OnSelectionChange 是 EditorWindow 的生命周期回调
    // 只要用户在 Hierarchy 或 Project 里点击了不同的对象
    // Unity 就会自动调用这个方法，不需要你手动触发
    void OnSelectionChange()
    {
        // Repaint 告诉 Unity：这个窗口的内容需要重新绘制
        // 它不会立刻绘制，而是标记一下，等 Unity 下一次刷新 UI 时执行
        // 如果不调用 Repaint，用户切换选中对象后窗口内容不会更新
        Repaint();
    }

    // ── 第三部分：绘制窗口 UI ────────────────────────────────────

    // OnGUI 是 EditorWindow 的绘制回调
    // Unity 每次需要刷新这个窗口时就调用它（包括上面 Repaint 触发的刷新）
    // 窗口里所有你能看到的内容，都在这里画出来
    void OnGUI()
    {
        // 获取当前在 Hierarchy 里选中的 GameObject
        GameObject go = Selection.activeGameObject;

        // 如果什么都没选，显示提示文字然后直接返回，后面的代码不执行
        if (go == null)
        {
            // EditorGUILayout.LabelField 在窗口里画一行只读文字
            // 类似于 HTML 里的 <label>，用户看得到但改不了
            EditorGUILayout.LabelField("请在 Hierarchy 中选中一个对象");
            return;
        }

        // 尝试从选中的 GameObject 上获取 EntityController 组件
        // GetComponent 你在 MMO 项目里用过，这里用法完全一样
        EntityController ec = go.GetComponent<EntityController>();

        // 如果这个 GameObject 上没有 EntityController，说明它不是我们关心的角色对象
        if (ec == null)
        {
            EditorGUILayout.LabelField("该对象没有 EntityController 组件");
            return;
        }

        // 走到这里说明：有选中对象 且 它有 EntityController 组件
        // 开始显示数据

        // EditorGUILayout.LabelField 有两个参数时：
        // 第一个参数是左边的标签（字段名）
        // 第二个参数是右边的值
        // 渲染出来类似 Inspector 里的只读字段：  Entity ID    42
        EditorGUILayout.LabelField("Entity ID", ec.entity.entityId.ToString());
        EditorGUILayout.LabelField("坐标", ec.transform.position.ToString());
    }
}