# ExtremeWorld MMO Client

Unity 2022.3.34f1c1，内置渲染管线（Built-in Render Pipeline），TCP 长连接 MMO 客户端。

---

## 目录结构

```
Assets/
├── Editor/           编辑器扩展脚本
├── FX/               粒子特效资源
├── Levels/           场景文件
├── Models/           3D 模型和动画
├── Resources/        动态加载资源（JSON 配置、UI 预制体）
├── Scripts/          全部 C# 源码（110 个文件）
│   ├── Assets/       Resloader — 资源加载封装
│   ├── Config/       Config — PlayerPrefs 读写（音乐/音效设置）
│   ├── Entities/     Entity / Character / Monster — 纯逻辑实体
│   ├── GameObject/   EntityController 等 — Unity 可视对象
│   ├── Log/          UnityLogger
│   ├── Managers/     14 个 Manager — 各子系统本地状态
│   ├── Models/       User / Item / BagItem / Quest — 数据模型
│   ├── Network/      NetClient — TCP 收发
│   ├── Scene/        SceneManager — 场景切换
│   ├── Services/     9 个 Service — 网络请求/响应处理
│   ├── Sound/        SoundManager / SoundDefine
│   ├── UI/           54 个 UI 脚本
│   └── Utilities/    Singleton / MonoSingleton / GameUtil / TimeUtil
├── Shader/           自定义着色器
├── Sound/            音频文件
├── ThirdParty/       JsonDotNet（Newtonsoft Json）
└── UI/               UI 预制体
```

**共享库**（不在 Assets 下）：
- `../Lib/Common/Network/MessageDistributer.cs` — 消息分发器，客户端服务端共用

---

## 核心架构

### 1. 网络消息数据流

```
NetClient (TCP 收包)
    ↓  MessageDistributer.ReceiveMessage()
MessageDistributer<NetClient>
    ↓  RaiseEvent<TMessage>() — 按消息类型分发
Service 层 (UserService / MapService / ItemService …)
    ↓  解析 Protobuf，调用 Manager
Manager 层 (CharacterManager / BagManager / QuestManager …)
    ↓  更新本地状态，触发 UnityAction / event
UI 层 (UIBag / UIQuest / UIChat …)
```

- **Service** 只做网络协议的收发和解析，不持有 Unity 对象。
- **Manager** 只维护本地游戏状态，不直接操作网络。
- 新增功能时：先在 Service 订阅/发送消息，再在 Manager 更新状态，最后在 UI 订阅 Manager 的事件。

### 2. Entity / GameObject 分离

```
Protobuf 网络数据 (NEntity / NCharacterInfo)
        ↓
逻辑层：Entity → Character / Monster        ← EntityManager 管理生命周期
        ↓  IEntityNotify 接口
表现层：EntityController（MonoBehaviour）    ← GameObjectManager 管理 GO 映射
```

- 逻辑实体和 Unity 对象通过 `IEntityNotify` 接口通信，双方不直接持有对方引用。
- `EntityManager` 负责创建/销毁逻辑实体；`GameObjectManager` 负责创建/销毁对应的 GameObject。
- 修改实体行为时优先改 `Entities/` 层，只有涉及动画/渲染才改 `GameObject/` 层。

### 3. Manager / Service 职责边界

| 职责 | 归属 |
|------|------|
| 发送网络请求 | Service |
| 订阅并解析网络响应 | Service |
| 维护本地列表/状态 | Manager |
| 通知 UI 刷新 | Manager（通过 UnityAction / event） |
| 操作 Unity GameObject | Manager / UI |

### 4. 单例体系

- `Singleton<T>`（`Utilities/Singleton.cs`）——普通 C# 类，所有 Manager / Service 继承。
- `MonoSingleton<T>`（`Utilities/MonoSingleton.cs`）——需要 MonoBehaviour 生命周期时用（NetClient、InputManager、SoundManager、LoadingManager、SceneManager）。`global = true` 时跨场景常驻。

### 5. 事件体系

| 层级 | 类型 | 场景 |
|------|------|------|
| UI 回调 | `UnityAction<T>` | 按钮点击、窗口状态变化 |
| 游戏逻辑 | `MessageDistributer` | 网络消息路由 |
| 网络连接 | `delegate` / `event` | 连接/断开通知 |

### 6. 数据加载

`DataManager`（Singleton）在启动时从 `Resources/` 读取 JSON，反序列化为字典供全局查询：
Maps / Characters / Npcs / Items / Shops / Equips / Quests / Rides / Monsters / SpawnPoints / SpawnRules / Teleporters。

---

## 命名规范

从现有代码归纳，新代码请保持一致：

| 类型 | 规范 | 示例 |
|------|------|------|
| 类名 | PascalCase | `CharacterManager`, `UIBagItem` |
| UI 窗口类 | `UI` 前缀 + PascalCase | `UIBag`, `UIQuestSystem`, `UICharInfo` |
| Manager 类 | `*Manager` 后缀 | `BagManager`, `GuildManager` |
| Service 类 | `*Service` 后缀 | `MapService`, `FriendService` |
| Controller 类 | `*Controller` 后缀 | `EntityController`, `RideController` |
| 公共方法 | PascalCase | `AddItem()`, `OpenWindow()` |
| 私有字段 | camelCase，无下划线前缀 | `currentTarget`, `syncInterval` |
| 公共属性 | PascalCase | `Instance`, `HasOpenUI` |
| 事件/委托字段 | `On` 前缀 + PascalCase | `OnCharacterEnter`, `OnGoldChanged` |
| 协议消息处理 | `On` + 消息名 | `OnUserLoginResponse()` |
| 常量/枚举值 | PascalCase | `SoundDefine.SFX_UI_CLICK` |
| 场景名 | PascalCase | `GameScene`, `LoginScene` |

---

## 开发约束

**渲染管线**
- 项目使用 Built-in Render Pipeline，不要建议迁移 URP 或 HDRP。所有 Shader、后处理、相机设置都基于内置管线。

**性能红线**
- 不要在 `Update()` 里调用 `GetComponent<>()`，在 `Awake()` / `Start()` 缓存引用。
- 不要在 `Update()` 里做字符串拼接（`+` 或 `$""`），改用 `StringBuilder` 或固定格式。
- 不要在 `Update()` / 高频路径里 `new` 对象或触发 GC，用对象池或结构体。

**第三方库**
- 只使用项目中已存在的第三方库：`JsonDotNet`（Newtonsoft Json）。不要引入 UniTask、DOTween、Odin 等项目中不存在的包。

**改动流程**
- 任何改动前先用文字描述方案（涉及哪些文件、改什么、为什么），等确认后再动手。
- 涉及网络协议（Protobuf 定义）的改动，需同时说明服务端影响。
