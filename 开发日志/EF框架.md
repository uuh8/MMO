# Q&A

### 1. 我看你的项目中使用了EF，你知道什么是EF吗？你为什么要使用EF？EF的原理是什么？你的项目中是如何使用的EF？

第一，我会先解释什么是 EF。Entity Framework 是 .NET 里的一个 **ORM（对象关系映射）框架**，用来在 **C# 对象和关系型数据库之间做映射**。简单说，就是我在代码里操作 `TUser、TCharacter、TCharacterBag` 这些 C# 类，EF 帮我把它们翻译成对 SQL Server 的增删改查，这样我不用在业务代码里手写大量 SQL 和 DataReader 逻辑。

第二，说清楚为什么用 EF 而不是自己写 SQL。在我的 MMO 服务端项目里，数据表比较多：用户、玩家、角色、角色物品、角色背包等等，实体之间还有一对多、一对一的关系。如果全部用 ADO.NET 手写 SQL，数据访问代码会非常臃肿，而且字段一改，所有 SQL 都要跟着改，**维护成本**很高。用 EF 的好处是：有强类型，字段改了编译期就能报错；可以用 LINQ 写查询，代码可读性好；它还能帮我做**关系导航**（例如 `character.Items`、`character.Bag`），对复杂对象图的增删改也比较自然，整体开发效率和可维护性都比手写 SQL 高。

第三，简单讲一下 EF 的原理。EF 的核心是一个**上下文类（DbContext）**，它相当于一个工作单元和对象仓库。我**通过上下文拿到实体集合**，例如 `context.TCharacters`，用 LINQ 写查询，EF 会把这个 LINQ 表达式解析成 SQL 发给数据库。上下文内部有一个“状态跟踪”的机制，记住每个实体是新增、修改还是删除，**当我调用 `SaveChanges()` 的时候，它会根据这些状态自动生成对应的 INSERT/UPDATE/DELETE 语句，在一个事务里提交到数据库**。这样业务层只关心“改了哪些对象”，具体 SQL 怎么拼、顺序怎么执行交给 EF。

最后，我会结合自己的项目说我是怎么用 EF 的。我的服务端是一个使用 SQL Server 的 MMORPG，**在数据访问层使用 EF 的数据库优先模式**，`Entities.edmx` 里建好 `TUser、TPlayer、TCharacter、TCharacterItem、TCharacterBag` 等实体和它们之间的关系（比如角色和背包一对一，角色和物品一对多），通过 .tt 模板生成对应的 C# 实体类和上下文。在业务层，比如玩家登录时，我会通过 EF 用用户名从 `TUser` 查到对应 `TPlayer` 和 `TCharacter`，再通过导航属性把角色的背包、物品一并加载出来；在游戏过程中角色位置变化、背包道具变更时，只是更新实体对象，最后统一调用一次 `SaveChanges()` 把这次操作中所有数据改动落库。整体上 EF 被我当成“统一的数据访问层”，上层服务只面对实体和仓储接口，不直接接触 SQL，这样项目结构比较清晰，后续扩展和维护都更方便。

------

我一般这么回答这一串问题，会分几层往下说，但用一口气的方式讲清楚。

首先我会先给一个概念上的定义：EF（Entity Framework）本质上是 .NET 里的一个 **ORM（对象关系映射）框架**，它做的事情就是**把关系型数据库里的表、字段、外键关系，映射成 C# 世界里的类、属性和导航属性**。对开发者来说，我写的是 `db.Characters.Where(c => c.Name == "xxx")` 这样的 LINQ 查询，EF 在背后把这段表达式翻译成 SQL，去 SQL Server 执行，把结果再组装回实体对象。这样我在业务代码里可以一直围绕“实体对象”和“领域逻辑”思考，而不是到处拼接 SQL 字符串。

为什么要用 EF？一个是开发效率和可维护性。我这个项目是一个 MMO，服务端有大量和数据库打交道的地方，比如用户、角色、背包、装备、任务、邮件等等，如果每张表都手写 CRUD + 手写 JOIN，很容易出错，而且表结构一变，SQL 全部跟着改，非常难维护。用 EF 后，数据库结构通过实体模型统一管理，改表只需要同步一次模型，后续用的是强类型访问，可以靠编译器帮我兜底。第二个原因是它支持 LINQ 查询，很多稍微复杂一点的过滤、投影、本来要拼半天 SQL 的东西，用 LINQ 表达出来既直观又容易重构。当然我也不会把 EF 当“黑盒”，性能敏感的地方会看它生成的 SQL，有些复杂统计会直接写原生 SQL 或存储过程。

它的原理，我一般会从“上下文 + 映射 + 查询翻译 + 变更跟踪”这几个关键词来讲。项目里会有一个 `Entities.Context.tt`，它相当于 EF 的工作单元（Unit of Work），里面有 `DbSet<TCharacter> Characters`、`DbSet<TUser> Users` 这种集合。启动的时候，EF 通过模型（edmx / Fluent API / 特性）知道“某个实体类对应哪张表，属性对哪一列，导航属性对应什么外键关系”。当我写 `db.Characters.Where(c => c.Id == id)` 这样的 LINQ 时，EF 不会在本地循环，而是把表达式树解析成 SQL：`SELECT ... FROM TCharacters WHERE Id = @id`，通过 provider 发给 SQL Server。查询出来后，它会把每一行数据按映射规则填充为 `TCharacter`，并放到 `Entities.Context.tt` 的跟踪缓存里。之后我修改实体，比如 `character.Level++`，上下文通过“变更跟踪”记录哪些属性发生了变化；调用 `SaveChanges()` 时，EF 会根据当前跟踪到的状态生成对应的 `INSERT/UPDATE/DELETE` 语句，放到一个事务里统一提交。这就是 EF 在用“对象操作”间接驱动 SQL 的基本机制。

> 只要 **edmx 发生变化**（字段、关系、实体数量……），保存时 `.tt` 模板就会自动重新生成 C# 实体类。
>
> - **从数据库更新模型（Update Model from Database）** → edmx 变了 → `.tt` 重新生成实体类
> - **从模型生成数据库（Generate DB from Model）** → 你修改了 edmx → `.tt` 重新生成实体类

至于我项目里具体怎么用 EF，我是把 EF 当作**最底层的“持久化层”**，和游戏逻辑、网络层是彻底分开的。数据库这边有一套 `TUser`, `TCharacter`, `TCharacterBag`, `TItem` 这样的实体类，对应 SQL Server 里的表，关系也通过 EF 配成了 1:1、1:N 等，比如 `TCharacter` 和 `TCharacterBag` 是 1:1，`TCharacter` 和 `TItem` 是 1:N。

> 例如：
>
> 玩家登录时，`UserService` 先通过 EF 查询用户和名下的角色列表；**进入游戏时，会把 `TCharacter` 映射成逻辑层的 `Character` 对象，再映射成网络层结构 `NCharacter` 发给客户端**。
>
> 玩家在线时，所有**实时状态**（位置、方向、血量）由内存里的 `Character`/`Map` 维护，不会每帧去查 EF；只有在**关键时刻才落盘**，比如创建角色、角色下线、定期保存、或者角色属性发生持久化变化（升级、背包变动）时，才把逻辑对象的状态回填到 `TCharacter` / `TItem`，然后通过 EF 的 `SaveChanges()` 写回数据库。
>
> 这样 EF 只负责“把内存里的最终状态可靠地同步到 SQL Server”，不会成为 MMO 实时逻辑的性能瓶颈，同时又保持了数据层的整洁和可维护性。