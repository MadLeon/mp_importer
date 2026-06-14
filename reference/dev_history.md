## 开发历史

### 限定编译输出为 64 位 Windows，消除多余 runtime 文件夹
- 在 `.csproj` 中添加 `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`，排除其他平台的原生库
- 添加 `<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>`，使输出直接位于 `net10.0-windows/` 而非嵌套的 `win-x64/`

### 修复首次点击 Extract 延迟，并澄清 DrawingNumber 来源
- 在 `App.OnStartup` 中启动后台线程预热 ClosedXML，消除第一次打开 Excel 的延迟
- 确认 `DrawingNumber` 已正确从单元格 J7 读取，与文件名相似是因为文件命名规范与图纸号一致

### 结果预览界面新增所有提取字段并支持编辑
- `ResultViewerViewModel` 新增 10 个可观察属性，覆盖全部头部信息字段
- `JsonResultService` 新增 `SaveAll` 方法，保存时完整序列化整个 `ExtractionResult`
- `ResultViewerWindow.xaml` 在进程步骤区上方增加 5 行 2 列的可编辑信息表单

### 修复数据库连接错误（SQLite Error 14）
- `ResolveDbPath` 向上搜索层数从 6 改为 8，使其能找到位于项目根目录的 `data/record.db`

### 重写数据库上传逻辑，支持自动创建 part 并按规则更新字段
- Part 不存在时自动 INSERT 新 part 并插入所有 process_template
- Part 存在时：revision/description 为空则 UPDATE + INFO log，不一致则 WARNING log 不修改
- Process_template 已存在时输出 WARNING log 跳过，不存在时插入
- `FindPartAsync` 仅按 `drawing_number` 匹配，按 `revision DESC` 排序取最新版本
- `MainViewModel.UploadDataAsync` 移除 overwrite 对话框，逻辑简化为直接调用 `UploadAsync`