# Git 工作流（Crown Siege Mod · 2 人团队）

> 本文是内部规范。给协作者看的版本在 `docs/协作者安装指南.md`，那份是自包含的，可以直接发。

---

## 0. 一句话原则

**Git 是唯一真相，项目盘是分发通道。** 代码和资源只活在 Git 里；项目盘只放「给非代码成员看的文档」和「可直接运行的产物」。

原因很硬：`AssetBundle`、`.entities`、`.psd` 这些二进制文件**没法 merge**。两个人同时改同一个二进制，Git 不会告诉你哪里冲突，它只会让后提交的把先提交的静默覆盖。所以这套流程的重点不是「怎么合」，而是**怎么从一开始就不撞**。

---

## 1. 仓库边界：什么进 Git，什么不进

| 内容 | 进 Git | 说明 |
|---|---|---|
| C# 源码 `src/` | ✅ | |
| 工具脚本 `tools/` | ✅ | |
| 设计/决策文档 `docs/` | ✅ | |
| 我们自己画的资源 `assets/` | ✅ 走 LFS | 必须是我们原创，不能是游戏里抠的 |
| 我们自己写的配置/JSON `data/` | ✅ | 纯文本，天然可 merge |
| 游戏程序集（`Assembly-CSharp.dll` 等） | ❌ | 版权 + 体积。靠 `local.props` 指向本机游戏目录 |
| 游戏原始资源（bundle / entities / 贴图音频） | ❌ | 同上。**一旦进仓库，仓库就不能公开了** |
| 编译产物 `bin/ obj/ releases/` | ❌ | 产物走项目盘分发 |
| `local.props`、`mod.config.local.json` | ❌ | 每人路径不同，必须 gitignore |
| 反编译出来的 `.cs` 全文 | ❌ | 用来读、用来对照，不落库 |

**判断标准一句话**：这个文件是不是「只有我能生成、别人拉下来也不该有」？是 → 不进。

---

## 2. 目录结构

```
06_CsMod/
├─ docs/                 设计、决策、研究笔记
│   ├─ GIT-WORKFLOW.md
│   ├─ 协作者安装指南.md
│   ├─ ARCHITECTURE.md   模块边界、引擎选型、联机为什么只定接口
│   ├─ DECISIONS.md      决策记录（ADR）
│   └─ RESEARCH.md       引擎分析、类名清单、Job 清单
├─ src/                  单一程序集，按目录分模块
│   ├─ Mod.csproj
│   ├─ Plugin.cs         入口（唯一会被两人同时改的文件，只追加不重排）
│   ├─ Core/             模块契约、注册表、配置、版本哨兵、日志
│   ├─ Diagnostics/      环境诊断
│   ├─ Menu/             主菜单注入 + 菜单探针
│   ├─ Control/          手动操控
│   └─ Net/              联机（只有接口）
├─ assets/               我们原创的美术/音频（LFS）
├─ data/                 我们自己的数据补丁文件
├─ tools/                detect-game.ps1、build-doc.py
├─ game-version.txt      目标游戏版本指纹（build-guid）
├─ Directory.Build.props 公共 MSBuild 配置
├─ local.props.example   路径配置模板
├─ .gitattributes        LFS 规则 + 二进制独占锁
└─ .gitignore
```

---

## 3. 初始化（只做一次，由 Lee 执行）

> **进度（2026-11-07）**：`git init` / `lfs install` / 首个 commit **已完成**（`ae2b1ba`）。
> 下面只剩 `git remote add` 和 `git push` 两步，等好友 GitHub 用户名与仓库名确定后执行。

```bash
cd <repo 目录>
git init
git lfs install
git add .
git commit -m "chore: 初始化项目骨架"
git remote add origin <远程仓库地址>
git push -u origin main
```

仓库设为 **Private**。理由：mod 源码里会有大量对游戏内部结构的分析记录，公开等于把拆解成果直接送人；等有可发布版本再决定是否开放。

---

## 4. 分支模型：主干 + 短分支

2 个人不值得上 GitFlow。用 trunk-based：

```
main ────●────●────●────●────●────  (永远可编译、可跑)
          \        /    \      /
           ●──●──●       ●────●        feat/xxx, probe/p0 这类短分支，活不过 2 天
```

规则：

1. **`main` 永远处于「拉下来就能编译、能进游戏」的状态。** 破了立刻修，别在上面继续叠加。
2. 任何超过一次提交的改动，开分支：`feat/<slug>` / `fix/<slug>` / `probe/<编号>`。
3. 分支寿命 ≤ 2 天。拖长了就会和对方的改动打架。
4. **禁止直接往 `main` 推**，唯一例外是 P0/P1/P2 探针（`probe/*` 可以直推，因为它的价值就是快速验证），推完立刻打标记。
5. 合并进 `main` 用 `git merge --no-ff`，保留合并点，方便日后 `git log --graph` 看清「哪个功能是一坨」。

---

## 5. 提交规范

格式：`type(scope): 一句话说清做了什么`

| type | 用途 |
|---|---|
| `feat` | 新功能 |
| `fix` | 修 bug |
| `probe` | P0/P1/P2 探针，实验性质 |
| `data` | 数据/配置改动（不改代码） |
| `docs` | 文档 |
| `chore` | 构建、依赖、杂务 |
| `revert` | 回滚 |

要求：

- **一次提交只干一件事。** 「顺手改了格式」「顺手调了个参数」全部拆开。
- 提交信息写**为什么**，不写「改了什么」——改了什么 `git diff` 会告诉你。
- 中文可以，别混着写。
- 提交前必须 `git status` 看一遍：有没有 `local.props`、有没有 `bin/`、有没有游戏 dll 混进去。

---

## 6. 两人分工与冲突预防

这是整套流程里最关键的段落。**分工按「文件所有权」切，不按「任务」切。**

| 区域 | 默认所有者 | 对方能不能碰 |
|---|---|---|
| `src/Core/**` `src/Menu/**` | Lee | 改动前先说一声 |
| `src/Control/**` | 协作者 | 改动前先说一声 |
| `src/Net/**` | 冻结，只读 | 改之前必须先过 D-007 |
| `src/Plugin.cs` | 双方 | 只追加模块注册行，不重排不重构 |
| `assets/**` | 美术负责人 | 只有本人碰 |
| `data/**` | 谁写的谁维护 | 按文件名切，一人一批 |
| `tools/**` | Lee | 只读使用 |
| `docs/**` | 双方 | 各自的笔记用 `docs/notes-<名字>.md` |

三条硬规矩：

1. **`assets/` 下的二进制文件，改之前先 LFS 加锁**：
   ```bash
   git lfs lock assets/units/dragon.psd     # 锁住
   git lfs unlock assets/units/dragon.psd   # 用完解锁
   ```
   `.gitattributes` 里已经给 `.psd / .blend / .aseprite` 标了 `lockable`，不加锁推不上去。这一步是唯一能物理阻止互相覆盖的机制，别省。

2. **同一份 `data/*.json` 不做并行修改。** 需要两人同时改就先拆文件：`units-dragon.json` 而不是共用 `units.json`。

3. **每天开工第一件事 `git pull --rebase`，收工最后一件事 `git push`。** 不允许「我本地攒了三天的改动」。

---

## 7. 每日循环（命令级）

```bash
# 1) 开工：同步
git switch main
git pull --rebase
git lfs pull

# 2) 开分支
git switch -c feat/astral-beam-cooldown

# 3) 写代码 / 改数据 → 本地自测（见第 9 节三条标准）

# 4) 提交
git status                 # 必须看一眼，确认没混入垃圾
git add src/Plugin.cs
git commit -m "feat(beam): 星束冷却改为按波次重置"

# 5) 推送
git push -u origin feat/astral-beam-cooldown

# 6) 开 PR，对方 review，合并
```

Review 要求（2 人也别省）：

- 必看三点：**有没有碰不该碰的文件**、**自测三条做没做**、**有没有把游戏资产带进仓库**。
- 批准后由**提交者本人**合并，避免两人同时点。
- 合并完删远程分支：`git push origin --delete feat/xxx`。

### 能力边界：Free 私有仓管不住「禁止直推 main」

**上面这套 review 规则，在 GitHub Free 的私有仓库里是「约定」，不是「强制」。**

已核实（GitHub Docs + Pricing，2026-11-07）：

- Free 个人账户支持**无限私有仓库 + 无限协作者**，2 人团队零成本。
- 但 `Protected branches`（禁止直推 main）和 `Required reviewers`（强制 review）**只对公开仓库免费**；私有仓要开这两项，得升 **Pro $4/月** 或组织 **Team $4/人/月**。

也就是说，Free 私有仓里对方**技术上可以直接 push main**，规范拦不住，只能靠双方自觉。

| 选项 | 代价 | 适用 |
|---|---|---|
| 甲 保持约定 | 0 | **推荐起步**：2 人互信，出事用 `revert` 兜底 |
| 乙 升 Pro | $4/月，仅 owner 付 | 真出现「手滑直推 main」再升，规范才落地 |
| 丙 建组织 + Team | $4/人/月 | 权限能细分到团队/角色，但 2 个人属于过度设计 |

### 协作者权限：能「只给开发权限」，不能「按目录授权」

个人账号下的私有仓库**权限粒度很粗**（官方口径）：

- 只有 Owner 和 Collaborator 两级。私有个人仓的协作者**只能给 Write**（读 + 推送），没有组织仓那套 Read / Triage / Maintain 分级。
- **「只给开发权限」是天然满足的**：删仓、改可见性、加协作者、改默认分支这些是 Owner 专属，好友拿不到。
- **不能按目录/文件授权**。GitHub 没有这个能力，好友能看到全部文件（含 `RESEARCH.md`）。CODEOWNERS 只是自动指派审阅人，不是访问控制。
- 授权是**按仓库**的，只影响这一个仓库，不会波及其他仓库。
- **移除协作者后，对方的私有 fork 会被删，但他本地 clone 里的数据拿不回来**。所以别把不想外泄的东西放进仓库。

---

## 8. 版本标记与发布

产物**不进 Git**，进项目盘。Git 里只留标记。

```bash
git tag -a v0.1.0+game.6000.6.7f1 -m "P0/P1/P2 探针通过，首个可安装包"
git push origin v0.1.0+game.6000.6.7f1
```

Tag 命名规则：`v<mod版本>+game.<游戏版本>`。后半段来自 `game-version.txt` 的 `unity=` 字段。

**这个后缀是必须的。** 游戏一更新，旧 tag 的包就可能在别人的机器上崩，必须能从 tag 一眼看出它对应哪个游戏版本。

发布流程：

1. `dotnet build src/Mod.csproj -c Release`
2. 打包成 `CrownSiegeToolkit-v0.1.0.zip`，内含 `BepInEx/plugins/CrownSiegeToolkit/CrownSiegeToolkit.dll` + `README.txt` + `CHANGELOG.md`
3. 上传到项目盘 `releases/` 目录（**上传前按项目规则先确认**）
4. 打 tag，写清这个版本对应哪个游戏版本

---

## 9. 什么叫「本地自测通过」

不能用「我跑了一遍没崩」当标准。三条全过才算：

1. **三次独立启动行为一致** —— 排除初始化竞态。关掉游戏重开，重复三次，现象相同。
2. **有日志断言** —— 关键路径打一行自己的日志，日志里的值符合预期。靠眼睛看画面不算。
3. **能一键回滚** —— 删掉 `BepInEx/plugins/CrownSiegeToolkit/` 就回到原状，且回滚后再启动一次，确认没有残留行为。

PR 描述里贴日志片段。没贴 = 没测。

---

## 10. 游戏更新时怎么办

`game-version.txt` 里的 `build-guid` 是哨兵。`tools/detect-game.ps1` 每次跑都会比对，不一致就警告。

游戏更新后的固定动作：

1. **冻结 `main`**，不许合并。
2. 从当前 `main` 拉 `game/<新版本号>` 分支。
3. 重跑 P0 / P1 / P2 三个探针，记录哪些挂了。
4. 修复通过后再合回 `main`，并把 `game-version.txt` 的 `build-guid` / `unity` 更新掉，提交信息写 `chore: 升级目标版本到 <新版本>`。
5. 旧版本的产物**不删**，在项目盘 `releases/` 里按版本留着。

> 注意：我们已经有公开证据表明这游戏会持续更新（作者原文提到新环境、新 boss、更多单位）。**不要假设「不会更新」**，这个假设一旦错了，所有跟版本绑定的东西一起废。

---

## 11. 回滚手册

| 场景 | 命令 | 注意 |
|---|---|---|
| 改坏了功能 | `git revert <sha>` | 不要用 `reset` 改历史 |
| 误提交了 `local.props` / dll | `git rm --cached <file>` + 补 `.gitignore` + 提交 | 历史里还留着，若涉及体积/版权再清理 |
| 本地改乱了想放弃 | `git reset --hard origin/main` | ⚠️ 会丢未提交改动，执行前 `git stash` 或确认 |
| 二进制资产被覆盖 | `git lfs checkout <sha> -- assets/x.png` 或 `git show <sha>:assets/x.png > x.png` | LFS 的历史版本能取回，这是 LFS 的最大价值 |
| 需要清理历史里的大文件/密钥 | `git filter-repo` | ⚠️ **改写历史必须先通知对方暂停推送**，之后所有人重新 clone |

**`git push --force` 只有一种情况允许**：清理误入的密钥或超大文件，且必须先在群里说一声、确认对方已推送完。其余任何情况都用 `revert`。

---

## 12. 禁止清单

- ❌ 往 `main` 直推（`probe/*` 除外）
- ❌ 提交 `local.props`、`bin/`、`obj/`、游戏 dll、游戏原始资源
- ❌ 在 `main` 上 `git push --force`
- ❌ 改别人 `assets/` 下的文件（除非对方明确交接）
- ❌ 不加锁就改 `lockable` 二进制
- ❌ 把「顺手改的格式」混进功能提交
- ❌ 攒三天不推
- ❌ 在仓库里放游戏的原始 bundle 或从游戏里提取的贴图

---

## 13. 项目盘（WorkBuddy）与 Git 的分工

| 用途 | 放哪 |
|---|---|
| 代码、资源、脚本、设计文档 | Git |
| 给协作者看的说明、给非技术成员看的材料 | 项目盘 |
| 可运行的发布包 `zip` | 项目盘 `releases/` |
| 每日进度、临时讨论结论 | 项目盘 `notes/` |

项目盘的所有写操作**每次都要人工确认**（这是硬规则），所以它天生不适合高频迭代。它适合「已经定稿、要给别人看」的东西。**别把它当网盘用。**
