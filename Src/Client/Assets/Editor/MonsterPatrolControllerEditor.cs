using UnityEditor;
using UnityEngine;

// 绑定到 MonsterPatrolController，在运行时显示调试信息
[CustomEditor(typeof(MonsterPatrolController))]
public class MonsterPatrolControllerEditor : Editor
{
    // OnInspectorGUI：在 Inspector 里追加运行时调试信息
    public override void OnInspectorGUI()
    {
        // 先画默认字段
        base.DrawDefaultInspector();

        // 只在运行时显示调试信息
        // Application.isPlaying 判断当前是否在运行游戏
        if (!Application.isPlaying) return;

        MonsterPatrolController controller = (MonsterPatrolController)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("运行时状态", EditorStyles.boldLabel);

        // 显示当前路径点数量
        // 这些数据是运行时注入的，编辑器没运行时看不到
        EditorGUILayout.LabelField("路径点数量",
            controller.PatrolPointCount.ToString());

        EditorGUILayout.LabelField("当前目标点索引",
            controller.currentIndex.ToString());

        EditorGUILayout.LabelField("巡逻方向",
            controller.isForward ? "→ 向终点" : "← 向起点");

        // 每帧刷新 Inspector 显示，让数据实时更新
        // 不加这行，Inspector 只有鼠标移上去时才刷新
        Repaint();
    }
}