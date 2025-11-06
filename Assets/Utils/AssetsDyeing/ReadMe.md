# Asset Dyeing 工具文档

## 一、工具简介

Asset Dyeing 是一个 Unity 编辑器工具，用于在运行时监控和管理项目资源。它提供了一个可视化界面，让你能够：

- **实时接收资源信息**：通过 `DyeingSo` ScriptableObject 从运行时环境接收资源路径信息
- **智能分类管理**：根据资源所在的文件夹路径自动分类显示资源
- **批量资源移动**：支持批量移动标记的资源到指定文件夹
- **路径配置持久化**：自动保存和恢复监控文件夹配置，无需每次重新设置

该工具特别适用于需要监控和分析项目资源使用情况的场景，例如资源依赖检查、资源整理等。

---

## 二、快速开始

### 2.1 打开工具窗口

1. 在 Unity 编辑器中，点击菜单栏 `YooAssetUtils` → `Asset Dyeing Window`
2. 工具窗口将自动打开

### 2.2 初始化 DyeingSo 资源

工具会自动查找并加载 `So/DyeingSo.asset` 文件。如果文件不存在，按以下步骤创建：

1. 在项目窗口右键点击 → `Create` → `YooAssetUtils` → `DyeingSo`
2. 将创建的 ScriptableObject 资源保存到 `Assets/Utils/AssetsDyeing/So/` 文件夹下，命名为 `DyeingSo`
3. 工具窗口会自动检测并加载该资源

### 2.3 配置监控文件夹

1. 点击工具窗口中的 **"Add Folder"** 按钮
2. 选择要监控的文件夹（必须在 Assets 目录下）
3. 配置文件夹选项：
   - **JoinCheck**：是否参与资源分类检查（勾选后，该文件夹下的资源会被分类显示）
   - **NotShow**：是否隐藏该文件夹下的资源（勾选后，该文件夹下的资源不会显示在列表中）

### 2.4 运行时发送资源信息

在运行时代码中，使用 `RuntimeSender` 组件或直接调用 `DyeingSo.SendMessageToEditor()` 发送资源路径：

```csharp
// 方式1：使用 RuntimeSender 组件
// 1. 创建一个 GameObject
// 2. 添加 RuntimeSender 组件
// 3. 在 Inspector 中拖入 DyeingSo 资源到 messenger 字段
// 4. 运行游戏，资源信息会自动发送到编辑器窗口

// 方式2：直接调用
DyeingObj obj = new DyeingObj();
obj.AssetPath = "Assets/YourFolder/YourAsset.prefab";
string message = JsonUtility.ToJson(obj);
dyeingSoInstance.SendMessageToEditor(message);
```

### 2.5 批量移动资源

1. 在资源列表中找到需要移动的资源
2. 勾选资源行的 **NeedMove** 复选框
3. 点击窗口底部的 **"BatchMoveAssets"** 按钮
4. 选择目标文件夹
5. 资源会被批量移动到目标文件夹，并自动更新路径信息

**注意**：批量移动操作只能在编辑器模式下执行（游戏未运行时）。

---

## 三、API 参考

### 3.1 AssetDyeingWindow 类

编辑器窗口主类，继承自 `OdinEditorWindow`。

#### 静态方法

##### `ShowWindow()`

打开 Asset Dyeing 工具窗口。

```csharp
public static void ShowWindow()
```

**使用示例**：
```csharp
AssetDyeingWindow.ShowWindow();
```

#### 公共属性

##### `messenger` (DyeingSo)

消息传递器，用于接收运行时发送的资源信息。

- **类型**：`DyeingSo`
- **说明**：工具会自动查找并加载 `So/DyeingSo.asset` 文件，也可手动指定

##### `ReportPaths` (List<ReportPathItem>)

监控文件夹路径列表。

- **类型**：`List<ReportPathItem>`
- **说明**：配置了需要监控的文件夹及其选项，支持自动保存和恢复

##### `stringItems` (List<StringItem>)

资源列表，显示所有接收到的资源信息。

- **类型**：`List<StringItem>`
- **说明**：包含资源的引用、路径、移动标记和所属文件夹信息

#### 内部类

##### `StringItem`

资源列表项数据结构。

```csharp
public class StringItem
{
    public UnityEngine.Object Asset;        // 资源对象的引用
    public string AssetPath;                // 资源的 Unity 相对路径
    public bool NeedMove;                   // 是否需要移动标记
    public string UnderFolder;              // 资源所属的文件夹名称
}
```

##### `ReportPathItem`

监控文件夹配置项。

```csharp
public class ReportPathItem
{
    public string Path;                     // 文件夹路径（绝对路径或相对路径）
    public bool JoinCheck;                  // 是否参与资源分类检查
    public bool NotShow;                    // 是否隐藏该文件夹下的资源
}
```

---

### 3.2 DyeingSo 类

用于运行时与编辑器通信的 ScriptableObject。

#### 公共属性

##### `MessageData` (string)

存储要传递的消息数据。

- **类型**：`string`
- **说明**：用于存储发送的消息内容

##### `OnMessageSent` (Action<string>)

消息发送事件。

- **类型**：`System.Action<string>`
- **说明**：当调用 `SendMessageToEditor()` 时触发此事件

#### 公共方法

##### `SendMessageToEditor(string message)`

从运行时向编辑器发送消息。

```csharp
public void SendMessageToEditor(string message)
```

**参数**：
- `message` (string)：要发送的消息，通常是 JSON 格式的 `DyeingObj` 序列化字符串

**使用示例**：
```csharp
DyeingObj obj = new DyeingObj();
obj.AssetPath = "Assets/MyFolder/MyAsset.prefab";
string json = JsonUtility.ToJson(obj);
dyeingSo.SendMessageToEditor(json);
```

---

### 3.3 DyeingObj 类

用于序列化资源路径的数据结构。

```csharp
[Serializable]
public class DyeingObj
{
    public string AssetPath;  // 资源的 Unity 相对路径
}
```

**字段说明**：
- `AssetPath`：资源的 Unity 相对路径，格式如 `"Assets/Folder/Asset.extension"`

**使用示例**：
```csharp
DyeingObj obj = new DyeingObj();
obj.AssetPath = "Assets/Resources/Prefabs/Player.prefab";
string json = JsonUtility.ToJson(obj);
```

---

### 3.4 RuntimeSender 类

运行时发送器组件，用于自动发送资源信息。

#### 公共属性

##### `messenger` (DyeingSo)

消息传递器引用。

- **类型**：`DyeingSo`
- **说明**：在 Inspector 中拖入创建的 DyeingSo 资源

#### 使用说明

1. 将 `RuntimeSender` 组件添加到场景中的任意 GameObject
2. 在 Inspector 中为 `messenger` 字段赋值（拖入 DyeingSo 资源）
3. 运行游戏，组件会在 `Update()` 中按配置的频率发送资源信息

**注意**：`RuntimeSender` 是一个示例实现，你可以根据实际需求修改发送逻辑。

---

## 使用技巧

1. **路径配置持久化**：工具会自动保存 `ReportPaths` 配置，关闭窗口后再次打开时会自动恢复
2. **自动资源加载**：工具会自动查找并加载 `So/DyeingSo.asset` 文件，无需手动指定（首次使用需创建）
3. **文件夹分类**：通过 `JoinCheck` 和 `NotShow` 选项可以灵活控制资源的显示和分类
4. **批量操作**：使用 `NeedMove` 标记资源后，可以一次性批量移动到目标文件夹
5. **路径格式**：支持绝对路径和相对路径（以 `Assets/` 开头），工具会自动转换

---

## 注意事项

- 批量移动操作只能在编辑器模式下执行（`EditorApplication.isPlaying == false`）
- 确保监控的文件夹路径在项目 Assets 目录下
- 资源路径必须是有效的 Unity 资源路径
- 移动资源时，如果目标路径已存在同名资源，操作会失败并记录错误信息

