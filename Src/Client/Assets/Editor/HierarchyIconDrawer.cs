using UnityEditor;
using UnityEngine;

[InitializeOnLoad]  // 告诉 Unity：编辑器启动时自动执行静态构造函数
public class HierarchyIconDrawer
{
    // 静态构造函数，类名后面加括号，没有返回值，没有访问修饰符
    static HierarchyIconDrawer()
    {
        // += 是委托的挂载语法，把 OnHierarchyGUI 挂到钩子上
        // 之后每次 Unity 绘制 Hierarchy 每一行时，都会调用 OnHierarchyGUI
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        // 第一步：从 instanceID 还原出 GameObject
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;

        // 第二步：判断这个对象是否符合你的条件
        // 这里以"有没有挂 EntityController 组件"为例
        if (go.GetComponent<EntityController>() == null) return;

        // 第三步：计算你想画的内容的位置
        // 在这一行的右边画一个小标签
        // selectionRect 是整行的区域，我们在它右侧偏移一点点
        Rect labelRect = new Rect(
            selectionRect.xMax - 50,  // 从这一行右边缘往左 50 像素
            selectionRect.y,          // 垂直位置和这一行对齐
            50,                       // 标签宽度 50 像素
            selectionRect.height      // 高度和这一行一样
        );

        // 第四步：在算好的位置画内容
        // GUI.Label 是最基本的绘制文字 API
        GUI.Label(labelRect, "[Entity]");
    }


}