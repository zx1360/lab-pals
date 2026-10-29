# 实验室同桌 · Lab Pals

安静陪伴学习的《命运石之门》Q 版 Windows 桌宠。支持冈部、真由理、红莉栖、琉华：键盘敲击、偶尔眨眼、点击回应、独立睡姿与专注计时。

<p>
<img src="./assets/characters/Okabe.png" width="22%" alt="冈部">
<img src="./assets/characters/Mayuri.png" width="22%" alt="真由理">
<img src="./assets/characters/Kurisu.png" width="22%" alt="红莉栖">
<img src="./assets/characters/Luka.png" width="22%" alt="琉华">
</p>


## 构建与使用

需要 Windows 10 / 11、系统自带的 .NET Framework 4.x，以及 Windows PowerShell 5.1 或 PowerShell 7。普通构建不需要 SDK、Node.js 或联网。

```powershell
.\build.ps1
.\dist\LabPals.exe
```

在项目目录中执行上述命令即可。构建必需的 `VERSION` 文件需与源码一起保留；生成的 `dist/LabPals.exe` 和缓存不会提交到 Git。

如果系统阻止直接运行脚本，可仅为这次构建使用：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

右键角色打开设置，左键点击回应、拖动移动，托盘图标可找回角色。详细操作见 [使用说明](docs/user-guide.md)。

## 技术与实现

| 技术 | 用途与选择理由 |
| --- | --- |
| C# 5 / .NET Framework | 使用系统编译器，生成便携单文件，减少安装依赖 |
| WinForms / Win32 | 管理透明窗口、托盘、拖动、设置面板和后台键盘输入 |
| GDI+ / 透明 PNG 图层 | 绘制角色和小幅手腕动画，保持实现与资源占用简单 |
| PowerShell | 构建、检查、预览、打包 |
| Node.js / Sharp（可选） | 离线整理美术和编码 GIF，不参与桌宠运行 |

键盘原始按下/松开事件进入状态机，立即压下对应的手，松开后平滑回弹。双手独立分层，衣服固定；点击切换脸部表情，睡眠使用单独画面。动作结束就停止高频刷新，待机只在偶尔眨眼等必要时唤醒。

设置保存在本机 `%LOCALAPPDATA%\LabPals\settings.xml`。只使用物理按键状态，不解析或保存输入文本；没有网络请求、声音或持续摆动。

## 项目结构

```text
src/          应用代码：Core 状态、Rendering 绘图、UI 窗口、Platform 系统接口
assets/       正式 PNG、图标、角色配置
artwork/      当前美术源图、基准图、提示词与分层记录
diagnostics/  自检、性能测试、预览导出
scripts/      开发与打包入口
tools/        可选离线美术工具
docs/         使用说明与角色扩展指南
VERSION       应用版本号
```

角色名称、配色、说明和手腕位置统一在 [assets/characters.json](assets/characters.json)，动画时长在 [AnimationTiming.cs](src/Core/AnimationTiming.cs)。新增角色通常只需要六张正式图层和一项配置，见 [角色扩展指南](docs/characters.md)。

## 开发

```powershell
.\scripts\test.ps1                  # 自检，独立于用户设置
.\scripts\preview.ps1               # 静态概览
.\scripts\package.ps1               # 检查后打包到 releases/
```

性能检查可加 `-Performance`。素材改动后可先运行 `scripts/validate-assets.ps1`。

需要重新处理原图或制作 GIF 时，才安装可选工具：

```powershell
npm install --prefix tools          # Node.js 20+
.\scripts\prepare-art.ps1 -Character Luka
.\scripts\preview.ps1 -EncodeAnimations
```

`.build/`、`artifacts/`、`dist/`、`releases/` 和依赖目录均已忽略，不提交到 Git。其他 Windows 设备克隆仓库后，直接构建即可；macOS / Linux 可以编辑源文件，但此实现的构建、运行和原生窗口检查依赖 Windows。

## 许可

代码、工具及文档使用 [MIT License](LICENSE)。角色图像是 AI 辅助制作的同人素材，不在 MIT 授权范围内；原作角色及相关权利归原权利方所有，见 [NOTICE](NOTICE)。
