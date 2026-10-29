# 维护约定

这是 C# 5 / .NET Framework / WinForms / GDI+ 的 Windows 桌宠。保持轻量、安静，避免加入常驻网页、持续帧循环或新的运行框架。

- 构建：`./build.ps1`。普通构建不依赖 SDK 或 Node.js。
- 行为或绘图改动：`./scripts/test.ps1`；性能相关改动加 `-Performance`。
- 美术改动：先 `./scripts/validate-assets.ps1`，再导出预览查看。
- 正式代码放 src，测试和导出工具放 diagnostics。
- VERSION 是版本号唯一来源；assets/characters.json 是角色资料与锚点唯一来源。
- 正式 PNG 放 assets/characters；美术输入和提示词放 artwork，生成的 assembled 不提交。
- 动画时长放 src/Core/AnimationTiming.cs。静止后停止高频刷新。
- 角色扩展约定见 docs/characters.md。已发布的角色 id 和 value 保持稳定。
- 代码使用 MIT；角色素材及原作相关权利见 NOTICE，不把它们笼统宣称为 MIT。
- 不把消息级测试或离线预览写成真实鼠标、Win+D 或桌面录屏已通过人工验收。
