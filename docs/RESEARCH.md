# 逆向研究记录（Crown Siege）

> 规则：**只记结论和依据，不贴反编译源码。** 需要复现时写清「用什么工具、点开哪个 dll、找哪个类」。
> 每条结论后面标了 [已核实] / [推断] / [待验证]。推断当事实用，是本项目最常见的死法。

采集环境：2026-11-07，Steam 正式版，安装路径 `D:\SteamLibrary\steamapps\common\Crown Siege`。

---

## 1. 构建指纹 [已核实]

| 项 | 值 | 依据 |
|---|---|---|
| 厂商 / 产品 | JOrbitsGames / CrownSiege | `CrownSiege_Data/app.info` |
| 引擎 | Unity **6000.6.7f1** | `ContentArchives/*.archive` 的 UnityFS 头 |
| 脚本后端 | **Mono**（非 IL2CPP） | 存在 `CrownSiege_Data/Managed/Assembly-CSharp.dll` + `MonoBleedingEdge/` |
| build-guid | `d07d7b203ec34f6b9a35c7796923a472` | `CrownSiege_Data/boot.config` |
| 托管程序集数 | 214 个 dll | `ls Managed/*.dll` |
| `Assembly-CSharp.dll` | 1,140,224 字节 | 同上 |
| Addressables | 2.11.2 | `StreamingAssets/aa/settings.json` |
| 本地化 | 9 语言（en/fr/de/pt-br/ru/zh-Hans/es-419/ja/ko） | `StreamingAssets/aa/StandaloneWindows64/localization-*` |

---

## 2. 内容与资源的存放形式

| 路径 | 实体 | 含义 |
|---|---|---|
| `StreamingAssets/ContentArchives/*.archive` | `UnityFS` 头（`xxd` 可见 `UnityFS....5.x.6000.6.7f1`） | **就是改了扩展名的 AssetBundle** |
| `StreamingAssets/ContentArchives/archive_dependencies.txt` | 文本 | 归档 → 对象（`b7aef056...:0`）的映射 |
| `StreamingAssets/EntityScenes/*.entities` | `DOTSBIN!` 头 | **DOTS 二进制子场景**，非文本、非 YAML |
| `StreamingAssets/aa/**` | Addressables bundles | 本地化表、monoscripts 等 |

**结论**：内容层不是松散文件，"加内容"必须走「重烘 ECS 子场景 + 重打 bundle + 重建 catalog」，需要匹配版本的 Unity 编辑器（见第 6 节）。

---

## 3. ECS / 脚本架构

`Assembly-CSharp.dll` 元数据字符串堆中出现（出现 ≠ 大量使用，字符串堆会去重）：

- `SystemBase` 存在、`ISystem` **不存在** → 系统继承自 **SystemBase（托管类）**，`OnUpdate` 是托管方法。**[已核实]**
- `BurstCompile`、`IJobEntity`、`IJobChunk` 均存在 → 计算被下沉到 Job 里。 **[已核实]**
- 原生库 `Plugins/x86_64/lib_burst_generated.dll` = 4,771,840 字节 → **Burst 使用量很大**。 **[已核实]**

已识别到的系统类名：

```
ApplyDamageSystem            ApplyPositionExplosionSystem
ProjectileSystem             SpawnProjectileSystem
ProjectileVisibilitySystem   ProjectileChildRenderersVisibilitySystem
SpawnImpactParticleSystem    PlayerSaveSystem
```

已识别到的 Job 结构体：

```
CollectDamageRequestsJob      CollectGameObjectDamageJob
CollectPendingDamageJob       CollectVisibilityRequestsJob
CollectVisibilityRequestsChunkJob
ProjectileMovementJob         UnitMovementIntegrationJob
SharedGroundSeparationJob     GroundContactJob
```

### 这意味着什么（关键）

| 想改的东西 | 可行性 | 手段 |
|---|---|---|
| 系统调度顺序、是否执行、传入参数 | ✅ 可行 | Harmony patch `SystemBase.OnUpdate` |
| **Job 内部的伤害/弹道算式** | ❌ 不可行 | Burst 编译为原生代码，Harmony 打不进去 |
| 数据资产（ScriptableObject / 配置） | ⚠️ 待验证 | 需先证明能读能写 bundle |
| 新增单位/关卡（ECS 子场景） | ⚠️ 成本极高 | 需重建 Unity 工程 + 同版本 Entities |

**所以「加机制」的边界是**：改规则流向可以，改算式不行。想让伤害公式变成别的样子，只能绕过 Job 在托管层重算，性能代价要单独评估。

---

## 4. 加载与 mod 支持现状

- 无 `mods/` 目录，无 Workshop 集成，`Plugins/` 只有 `Steamworks.NET` 与 `steam_api64.dll`。 **[已核实]**
- 无 BepInEx / Harmony 痕迹 → 我们是第一个往这个游戏里注入的人，没有前人踩过的坑可以抄。 **[已核实]**
- `Plugins/x86_64/libonigwrap.dll` 存在（Oniguruma 正则库），可能用于文本/配置解析，值得后续跟。 **[推断]**

---

## 5. 外部事实（SteamDB，2026-11-07）

| 指标 | 值 |
|---|---|
| 发售日 | 2026-09-18 |
| 全史峰值同时在线 | 1,270（2026-09-19） |
| 当前同时在线 | 31 |
| 24h 峰值 | 36 |
| 评价 | 434 条，80.4% 好评 |

**推论**：日活两位数。任何「社区分发」的预期都要按这个基数算，不能按愿望算。

开发者公开表态（itch devlog 原文）：*"We have so many plans for the full release, including new environments, more boss battles and loads of units and enemies."* → **游戏会持续更新，不要假设版本冻结。**

---

## 6. 待验证清单

| # | 假设 | 验证方法 | 致命度 |
|---|---|---|---|
| 1 | BepInEx 6 BE (Mono) 能在 Unity 6000.6 上注入 | P0 探针 | 致命 |
| 2 | Harmony 能 patch 到 SystemBase.OnUpdate | P2 探针 | 致命 |
| 3 | 系统能读到并修改 ScriptableObject / 配置数据 | P1 探针 | 高 |
| 4 | 用 Unity 6000.6.7f1 能重烘 bundle 且被游戏接受（catalog 校验） | 造空 bundle 塞回去 | 高 |
| 5 | 分布式产物不被 Steam 校验破坏 | 装完跑一次校验 | 中 |

---

## 7. 工具链记录

- 反编译：ILSpy（`winget install ICSharpCode.ILSpy`）
- 快速字符串取证：`grep -a -o "<pattern>" Assembly-CSharp.dll`
  注意 1：元数据字符串堆会去重，计数只代表「存在」，不代表使用次数。
  注意 2：**方法名/类型名在 `#Strings` 堆里是 UTF-8**（可以直接 grep），
  **字符串字面量在 `#US` 堆里是 UTF-16LE**（要 `s.encode('utf-16-le')` 才 grep 得到）。
- 二进制头：`xxd -l 96 <file>`
- 构建：`dotnet build src/Mod.csproj`（.NET SDK 10.0.403 已验证通过）

### 编译踩坑（2026-11-07 实测，三个都踩过）

| 报错 | 真实原因 | 解法 |
|---|---|---|
| `CS0433` 类型同时存在于 `UnityEngine` 和 `UnityEngine.CoreModule` | NuGet 的 `UnityEngine` 5.6.1 **桩包**（BepInEx.Unity.Mono 的传递依赖）和游戏 CoreModule 都定义了同名类型 | 显式 `<PackageReference Include="UnityEngine" ... ExcludeAssets="compile;runtime" />` 排除桩包的编译引用 |
| `CS0012` `MonoBehaviour` 在未引用的程序集中定义 | 只引了 CoreModule，缺 `UnityEngine.dll`——`BaseUnityPlugin` 是绑在名为 `UnityEngine` 的程序集上的 | 必须同时引 `UnityEngine.dll` |
| `CS1069` `Application` 已转发到 `UnityEngine.CoreModule` | 只引了 `UnityEngine.dll`——它是**纯类型转发门面**，自己不定义任何类型 | 同上，两个都要引 |

**结论：`UnityEngine.dll`（门面）+ `UnityEngine.CoreModule.dll`（定义）+ 排除 NuGet 桩包，三件缺一不可。** 这条坑不写下来，下一个人一定会再踩一遍。

> 反直觉的点：`UnityEngine.dll` 既不「包含全部类型」（它只转发），也不「可以不要」（它提供程序集标识）。两种误解各对应一个报错，而且报错信息互相矛盾，靠猜是猜不出来的。
