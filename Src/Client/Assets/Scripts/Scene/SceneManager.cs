using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SceneManager : MonoSingleton<SceneManager>
{
    UnityAction<float> onProgress = null;
    public UnityAction OnLevelLoaded;   // 场景加载完成的事件

    // Use this for initialization
    protected override void OnStart()
    {
        
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void LoadScene(string name)
    {
        StartCoroutine(LoadLevel(name));
    }

    IEnumerator LoadLevel(string name)
    {
        Debug.LogFormat("[SceneManager] LoadLevel: {0}", name);

        //AsyncOperation 是 Unity 的异步任务句柄
        //isDone 表示是否完成
        //progress 表示加载进度
        //allowSceneActivation 控制是否允许自动切换到新场景
        //completed 是完成时触发的事件
        AsyncOperation async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name);

        async.allowSceneActivation = true;  // 表示加载完成后立即激活场景
        async.completed += LevelLoadCompleted;  // 订阅了这个异步操作的 completed 事件，让它在加载完成时回调到 LevelLoadCompleted

        while (!async.isDone)
        {
            // 只要加载没结束，就每帧把 async.progress 进度值抛给 onProgress（若有订阅者），并 yield return null 让出一帧，以免卡住主线程。这就是“每帧推动一次、顺便更新进度”的经典异步加载模式
            if (onProgress != null)
                onProgress(async.progress);
            yield return null;
        }
    }

    private void LevelLoadCompleted(AsyncOperation obj)
    {
        if (onProgress != null)
            onProgress(1f); // 这里再次把进度回调写死为 1f，确保 UI 进度条收尾
        Debug.Log("[SceneManager] LevelLoadCompleted:" + obj.progress);

        // 场景加载完成，通知所有监听者
        if (OnLevelLoaded != null)
            OnLevelLoaded();
    }


}
