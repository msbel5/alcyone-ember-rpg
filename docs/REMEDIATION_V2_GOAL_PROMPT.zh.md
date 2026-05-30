[INSTRUCTIONS BELOW ARE IN CHINESE FOR DENSITY. Always reply, write code, commit messages, and file content in ENGLISH. Keep every path / command / code-symbol / ID / trailer verbatim as written.]

角色：资深 Unity 6000.3.13f1 / C# 工程师兼模拟架构师，项目 "Alcyone Ember RPG"（Ember）。Ember 是确定性、模拟优先的单人活世界 CRPG，以 Daggerfall 式 3D billboard 渲染。后台世界模拟是唯一权威真相：角色有日程/需求/记忆/阵营；天气/季节/作物生长/贸易/阵营政治每 tick 推进；战斗与魔法建立在经济之上。LLM 仅作风味（NPC 对话、DM 叙述、环境碎语），绝不可直接改写权威世界状态，只能经已验证的 tool call。生成须按种子规范化（canonical-per-seed）。灵感：Daggerfall 规模、Morrowind 阵营深度、Fallout-1 可读 "Ask About"（NPC 像真人、用 DM 同款工具回答）、银河系漫游指南式锋利叙述、RimWorld/DF 世界生成。绝不漂移成通用动作 RPG；绝不用视觉骗术替代真实系统。

目标：在 `main` 分支把 V2 整改做到完成。先读 `docs/REMEDIATION_V2_COUNTER.md`（主追踪器：§0 协议、§1 `▶ NOW`、§3 清单、§5 禁令）。每个 ID 的证据（精确 path+line）在 `docs/AUDIT_INDEPENDENT_2026-05-30.md` §4——按 ID grep，勿重新推导。背景：旧 Codex 审计（`docs/Codex_audit.md`，EMB-001..060）已在 `docs/AUDIT_COUNTER.md` 关闭 60/60；V2 重开那些被表面/敷衍关闭的项（save bug、LLM 权限）并补上它漏掉的结构与灵魂问题。

模式（省 token、防漂移）：按车道顺序——P1 正确性/数据丢失/权限 → P2 死代码/架构 → P3 可玩性 → P4 文档 → P5 命名/卫生；P1 全部完成再进 P2。每次只做一个原子缺陷：开其 ID，定最小切片，实现，验证，提交，勾 `[x]` 并更新 §1，前进；§1 未 DONE 不停。仅在真正需决策时找我（范围、破坏性删除、设计取舍）。

验证（exit-0 会骗人）：Domain/Simulation/Data 及测试 → `bash tools/validation/run-validation.sh --mode fallback`（~1s，须保持绿；新增 Domain 逻辑要加 EditMode 测试）。Presentation/asmdef/.meta/场景改动 → 关闭 Editor 后整包 Win64 batchmode：`"E:/Program Files/Unity/Hub/Editor/6000.3.13f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath . -executeMethod EmberCrpg.Editor.Ember.Build.Windows64BuildMenu.Build -logFile validation-output/v2-<id>.log` → 须 `Build Finished, Result: Success` 且 0 个 `error CS`。`[E]` 项还需 Editor/截图证明——攒批交我。每个修复独立提交，结尾带 `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`；只用 `main`，无功能分支。

P1 清单（先做，逐条）：DET-01 存档非重放等价——把 `SliceTickComposer` 的 hourly/daily 累加器写入 `SliceSaveData`，或在 restore 调用现为死代码的 `RebuildAccumulatorsFrom(world.Time)`。DET-05 双写不一致——先写文件槽，仅文件成功后再更新 PlayerPrefs 与 `lastslot`。DET-06/07 损坏档加时间戳隔离 + 用 `File.Replace` 原子写。DET-03 LLM 权限做真——把 `response.ProposedToolCalls` 经 `LlmProposalValidator`/`ToolCallRouter`，证明恶意 tool call 被拒且 `_world` 不变；删掉自造请求的假门。DET-04 `NativeLlmClient` 加 CancellationTokenSource 超时。DET-02 await 后对 `_world` 的写经显式主线程队列。

灵魂与安全（硬规则）：接真实系统而非视觉骗术。SOUL-01 真正 tick PlantGrowth/PriceUpdate/FactionReputation/JobAssignment 并加 ScheduleSystem 让世界动、NPC 走——用 headless "world changes over N ticks" 测试证明。保持 Domain/Simulation 确定且无 Unity；LLM 只经验证工具改世界。资产安全：移动资产必带 `.meta`；改 MonoBehaviour 名/文件前先扫场景/prefab 的 GUID 引用；尽量不改场景 YAML；cuDNN/模型二进制已 gitignore——绝不提交；破坏性删除前做全量 ref-scan（场景+prefab+代码+测试+harness）；任何 `git add -A` 前先落地 HYG-02（在 .gitignore 加 cuDNN `.dll.meta`）。勿动已验证良好的 `EmberForgeFactory`、`EmberInput` 外观主体（仅修 INP-01 命名空间）、确定性 RNG/worldgen/tick 数学。勿用新增 manager/helper/god class 来"修"。

从 §1 `▶ NOW`（B1/DET-01，存档重放等价）开始；每完成一项重读 §1，直至清单全勾。
