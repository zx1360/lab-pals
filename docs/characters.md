# 新增角色与调整动作

## 一份角色资料

assets/characters.json 是角色资料的唯一来源。每项包含：

| 字段 | 含义 |
| --- | --- |
| id | 稳定的英文标识，同时对应 PNG 文件名前缀和保存的角色枚举名称 |
| value | 稳定且唯一的非负整数枚举值 |
| name / shortName / english | 全名、设置按钮短名及预览英文名 |
| traits / reactionCaption | 外观特征与点击回应的预览说明 |
| accent | #RRGGBB 形式的柔和强调色 |
| leftWrist / rightWrist | 320×280 设计坐标中的手腕锚点 |

已发布的 id 和 value 不重命名、不重排含义，避免改变旧设置的角色身份。JSON 列表顺序决定设置和预览的显示顺序。

构建自动生成枚举与不可变目录，程序运行时无需读取 JSON。不要编辑 .build/generated/ 中的临时代码。

## 六张正式图层

每位角色提供以下六张 640×560 的透明 PNG，放入 assets/characters/。文件名前缀替换为角色 id。

| 文件 | 内容 |
| --- | --- |
| Luka.png | 完整待机画面 |
| Luka-rest.png | 闭眼版本，衣物、手部、轮廓与主图一致 |
| Luka-left.png | 原画布坐标中的左手遮罩与像素 |
| Luka-right.png | 同上，右手 |
| Luka-reaction.png | 仅脸部的点击表情，其他区域透明 |
| Luka-sleep.png | 独立睡姿，与原角色占地及下缘保持协调 |

左右按画面方向命名。手部层包含皮肤和必要描边，避免把袖口和衣褶放进活动层。手腕活动以锚点旋转及微小拉伸完成，身体固定。运行时在显示尺寸拆分手部，可保持静止画面与主图像素一致。

默认待机是稳定的姿势，只有偶尔眨眼。点击回应控制在短时间内，不加大幅摆动、常驻对白或粒子。睡稳后保持静止。

## 添加流程

1. 准备以上六个文件及正确锚点。
2. 在 characters.json 追加一项，使用新 id 和新 value。
3. 运行 scripts/validate-assets.ps1，然后运行 scripts/test.ps1。
4. 运行 scripts/preview.ps1，检查默认小号尺寸下的轮廓、手腕接缝、表情和睡姿。
5. 原图、参考图和提示词保留在 artwork/，记录来源与分层参数；运行素材始终放在 assets/characters/。

设置按钮按四列自动换行，各角色概览及动画预览按目录数量扩展。不需要在窗口、绘图、测试和预览代码中各加一套姓名数组或手腕表。

## 调整现有美术与动作

当前离线准备工具 tools/art/prepare-sprites.cjs 为现有 Q 版构图服务，对齐头发/帽子和服装，在下方区域接入新手形，并截取脸部表情。它假设现有的头部位置和肤色范围；构图明显不同的新角色应准备合格的六个正式图层，或明确调整分层参数，不能直接假定自动分层正确。

原始编辑图在 artwork/source/，基准图在 artwork/reference/，提示词和分层记录也在 artwork/。离线准备工具会生成 artwork/assembled/ 供查看；该目录不提交到 Git。

动作时长在 src/Core/AnimationTiming.cs；手腕幅度在 src/Rendering/SpriteRig.cs；状态及触发规则在 src/Core/PetBrain.cs。调整后检查松开插值、双手独立、衣物固定、点击与打字同时响应以及睡稳后停止高频刷新。
