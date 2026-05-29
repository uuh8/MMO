using UnityEditor;
using UnityEngine;

// 绑定到 SpawnPoint 组件，替换它的默认 Inspector 并扩展 Scene 视图交互
[CustomEditor(typeof(SpawnPoint))]
public class SpawnPointEditor : Editor
{
    // 缓存 patrolPoints 字段的序列化数据，OnEnable 里找一次，避免每帧重复查找
    private SerializedProperty patrolPointsProp;

    void OnEnable()
    {
        // FindProperty 通过字段名字符串找到对应的序列化字段
        // 字段名必须和 SpawnPoint.cs 里的字段名完全一致
        patrolPointsProp = serializedObject.FindProperty("patrolPoints");
    }

    // OnInspectorGUI：控制 Inspector 面板长什么样
    public override void OnInspectorGUI()
    {
        // 先画默认的所有字段（ID、speed、viewRadius 等）
        base.DrawDefaultInspector();

        EditorGUILayout.Space();

        // 在默认字段下面追加一个路径点管理区域
        EditorGUILayout.LabelField("路径点管理", EditorStyles.boldLabel);

        // 显示当前路径点数量
        EditorGUILayout.LabelField("当前路径点数量", patrolPointsProp.arraySize.ToString());

        EditorGUILayout.BeginHorizontal();

        // 添加路径点按钮
        // 点击后在列表末尾插入一个新的 Vector3，初始值为怪物当前位置
        if (GUILayout.Button("添加路径点"))
        {
            // 注册 Undo，让 Ctrl+Z 能撤销这次添加
            Undo.RecordObject(target, "添加路径点");

            // 在数组末尾插入一个新元素
            patrolPointsProp.InsertArrayElementAtIndex(patrolPointsProp.arraySize);

            // 把新路径点的初始位置设为 SpawnPoint 自身的位置，而不是 (0,0,0)
            // 这样策划添加路径点后，拖拽起点在怪物脚下，比较直观
            SpawnPoint sp = (SpawnPoint)target;
            patrolPointsProp
                .GetArrayElementAtIndex(patrolPointsProp.arraySize - 1)
                .vector3Value = sp.transform.position;

            serializedObject.ApplyModifiedProperties();
        }

        // 删除最后一个路径点按钮
        // 只有列表里有路径点时才允许点击
        EditorGUI.BeginDisabledGroup(patrolPointsProp.arraySize == 0);
        if (GUILayout.Button("删除最后路径点"))
        {
            Undo.RecordObject(target, "删除路径点");
            patrolPointsProp.DeleteArrayElementAtIndex(patrolPointsProp.arraySize - 1);
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        // 路径点不足两个时显示红色警告
        if (patrolPointsProp.arraySize < 2)
        {
            EditorGUILayout.HelpBox("至少需要两个路径点才能巡逻", MessageType.Warning);
        }

        // 一键清空所有路径点按钮
        if (GUILayout.Button("清空所有路径点"))
        {
            Undo.RecordObject(target, "清空路径点");
            patrolPointsProp.ClearArray();
            serializedObject.ApplyModifiedProperties();
        }
    }

    // OnSceneGUI：控制 Scene 视图里的可交互内容
    void OnSceneGUI()
    {
        if (patrolPointsProp.arraySize == 0) return;

        for (int i = 0; i < patrolPointsProp.arraySize; i++)
        {
            // 读取第 i 个路径点的当前坐标
            Vector3 point = patrolPointsProp.GetArrayElementAtIndex(i).vector3Value;

            // 在路径点旁边显示序号标签，让策划知道这是第几个点
            Handles.Label(point + Vector3.up * 0.5f, "P" + i);

            // 开始检测用户是否拖动了控制柄
            EditorGUI.BeginChangeCheck();

            // 在路径点位置画出可拖拽的三轴移动控制柄
            // 返回值是拖拽后的新位置
            Vector3 newPos = Handles.PositionHandle(point, Quaternion.identity);

            // EndChangeCheck 返回 true 说明用户拖动了
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "移动路径点");

                // 从路径点正上方向下做射线检测，找到地形表面
                // 为什么起点要往上 10 米？因为路径点可能已经在地面以下了，如果从路径点本身向下发射，射线起点已经穿过地形，就检测不到地形表面了。所以先往上抬高一段距离，保证射线起点一定在地形上方。
                if (Physics.Raycast(
                    newPos + Vector3.up * 10f,  // 射线起点：路径点位置往上 10 米
                    Vector3.down,               // 射线方向：向下
                    out RaycastHit hit,         // 输出参数：碰撞结果存在这里，RaycastHit 是一个结构体，里面存着碰撞的详细信息：
                        //hit.point    // 碰撞点的世界坐标（地形表面的位置）
                        //hit.normal   // 碰撞面的法线方向（地形在碰撞点的朝向）
                        //hit.distance // 射线起点到碰撞点的距离
                        //hit.collider // 被碰到的 Collider 组件
                    20f                         // 最大检测距离：20 米
                ))
                {
                    // 把 Y 坐标修正到地形表面
                    newPos.y = hit.point.y;
                }

                patrolPointsProp.GetArrayElementAtIndex(i).vector3Value = newPos;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}