using UnityEditor;
using UnityEngine;

public class HierarchyTool
{
    [MenuItem("GameObject/我的工具/打印对象名称")]
    static void PrintSelectedName()
    {
        GameObject go = Selection.activeGameObject;
        if (go != null)
            Debug.Log(go.name);
    }

    [MenuItem("GameObject/我的工具/打印对象名称", true)]
    static bool ValidatePrintSelectedName()
    {
        // 只有选中了 GameObject 才允许点击
        return Selection.activeGameObject != null;
    }
}