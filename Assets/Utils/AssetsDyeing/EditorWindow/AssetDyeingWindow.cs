using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

public class AssetDyeingWindow : OdinEditorWindow
{
    [MenuItem("YooAssetUtils/Asset Dyeing Window")]
    public static void ShowWindow()
    {
        GetWindow<AssetDyeingWindow>("Asset Dyeing");
    }

    [FormerlySerializedAs("Messenger")]
    [Title("Settings")]
    [InfoBox("Assign the DyeingSo asset if needed. Below you can manage .report paths and the display list.", InfoMessageType.Info)]
    [AssetSelector]
    [OnValueChanged(nameof(OnMessengerChanged), true)]
    [LabelText("Messenger Asset")]
    public DyeingSo messenger;

	[Title("Report Paths"), PropertyOrder(10)]
	[ListDrawerSettings(
		DraggableItems = false,
		Expanded = true,
		ShowIndexLabels = false,
		CustomAddFunction = nameof(AddReportPath),
		CustomRemoveIndexFunction = nameof(RemoveReportPath),
		OnEndListElementGUI = nameof(AfterReportPathElementGUI))]
	[OnValueChanged(nameof(OnReportPathsChanged))]
	[LabelText("Paths (.*.report)")]
	public List<ReportPathItem> ReportPaths = new List<ReportPathItem>();

    [Button("Add Folder"), PropertyOrder(11)]
    private void AddReportPathViaPicker()
    {
        AddReportPath();
    }
    
    [TableList(
         ShowIndexLabels = false,
         AlwaysExpanded = true
         // IsReadOnly = true
         ), PropertyOrder(20)]
    public List<StringItem> stringItems = new List<StringItem>();

    [System.Serializable]
    public class StringItem
    {
        public StringItem()
        {
            
        }
        [LabelText("AssetObject")]
        [TableColumnWidth(100)]
        [ReadOnly]
        [PreviewField(30, ObjectFieldAlignment.Left)]
        public UnityEngine.Object Asset;
        
        [LabelText("AssetPath")]
        [TableColumnWidth(100)]
        [ReadOnly]
        [DisplayAsString]
        public string AssetPath;
        
        public bool NeedMove = false;
        
        [ReadOnly]
        [DisplayAsString]
        public string UnderFolder = "这是绿色的文字";
    }

    private readonly HashSet<DyeingSo> subscribed = new HashSet<DyeingSo>();
    private const string ReportPathsPrefsKey = "AssetDyeingWindow_ReportPaths";
    private const string MessengerPathPrefsKey = "AssetDyeingWindow_MessengerPath";
    private bool isLoadingReportPaths = false;

    [System.Serializable]
    private class ReportPathsData
    {
        public List<ReportPathItem> paths = new List<ReportPathItem>();
    }

    private void OnEnable()
    {
        if (messenger == null)
        {
            // 先尝试从 EditorPrefs 加载保存的路径
            string savedPath = LoadMessengerPath();
            if (!string.IsNullOrEmpty(savedPath))
            {
                messenger = AssetDatabase.LoadAssetAtPath<DyeingSo>(savedPath);
            }
            
            // 如果从缓存加载失败，尝试使用默认路径
            if (messenger == null)
            {
                string dyeingSoPath = GetDyeingSoPath();
                if (!string.IsNullOrEmpty(dyeingSoPath))
                {
                    messenger = AssetDatabase.LoadAssetAtPath<DyeingSo>(dyeingSoPath);
                }
            }
        }
        TrySubscribe(messenger);

        // 从本地缓存加载 ReportPaths
        LoadReportPaths();

        //Test
        // DoAddReportPath("Assets/DemoResources/TestAssets/UIs", true, false);
        // DoAddReportPath("Assets/DemoResources/TestAssets/Arts", false, false);
        //
        string assetPath1 = "Assets/DemoResources/TestAssets/Arts/test.jpg";
        string assetPath2 = "Assets/DemoResources/TestAssets/UIs/ImageTest.prefab";
        AddAsset(assetPath1);
        var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath2);
        AddAsset(obj);
    }

    private void OnDisable()
    {
        TryUnsubscribe(messenger);
        
        // 保存 ReportPaths 到本地缓存
        SaveReportPaths();
    }

    private void OnMessengerChanged()
    {
        // 保存 messenger 路径到 EditorPrefs
        SaveMessengerPath();
        
        // Resubscribe when the Messenger reference changes
        foreach (var so in new List<DyeingSo>(subscribed))
        {
            TryUnsubscribe(so);
        }
        TrySubscribe(messenger);
        Repaint();
    }

    private void TrySubscribe(DyeingSo so)
    {
        if (so == null)
        {
            Debug.LogError("DyeingSo so is null");
            return;
        }
        if (subscribed.Contains(so)) return;
        so.OnMessageSent += HandleMessage;
        subscribed.Add(so);
    }

    private void TryUnsubscribe(DyeingSo so)
    {
        if (so == null)
        {
            return;
        }
        if (!subscribed.Contains(so)) return;
        so.OnMessageSent -= HandleMessage;
        subscribed.Remove(so);
    }

    private void HandleMessage(DyeingObj message)
    {
        // DyeingObj obj = JsonUtility.FromJson<DyeingObj>(message);
        AddAsset(message.Asset);
        Repaint();
    }

    // ===== Helpers for ReportPaths =====
	private void AddReportPath()
    {
        var path = EditorUtility.OpenFolderPanel("Select folder", Application.dataPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            DoAddReportPath(path);
        }
    }

    private void DoAddReportPath(string path, bool joinCheck = true, bool notShow = false)
    {
        if (!ReportPaths.Exists(p => p != null && p.Path == path))
        {
            ReportPaths.Add(new ReportPathItem
            {
                Path = path,
                JoinCheck = joinCheck,
                NotShow = notShow
            });
            SaveReportPaths(); // 保存到缓存
        }
    }

	private void RemoveReportPath(int index)
    {
        if (index >= 0 && index < ReportPaths.Count)
        {
            ReportPaths.RemoveAt(index);
            SaveReportPaths(); // 保存到缓存
        }
    }

	[System.Serializable]
	public class ReportPathItem
	{
		[HorizontalGroup("rp")]
		[Sirenix.OdinInspector.FolderPath(AbsolutePath = true)]
		[HideLabel]
		public string Path;

        [HorizontalGroup("rp", Width = 0.2f)]
        [LabelText("JoinCheck")]
        public bool JoinCheck;

        [HorizontalGroup("rp", Width = 0.2f)]
        [LabelText("IncludeChildFolders")]
        public bool IncludeChildFolders;

		[HorizontalGroup("rp", Width = 0.2f)]
		[LabelText("NotShow")]
		public bool NotShow;
	}

	private void AfterReportPathElementGUI(int index)
	{
		GUILayout.Space(6);
	}

    [System.Serializable]
    public class DisplayItem
    {
        [ShowInInspector, TableColumnWidth(28, Resizable = false)]
        [PropertyOrder(0)]
        [LabelText("")]
        [HideLabel]
        public Texture Status => GetStatusTexture();

        [ReadOnly]
        [LabelText("Text"), PropertyOrder(1)]
        public string Text;

        [ReadOnly]
        [LabelText("Extra"), PropertyOrder(2)]
        public string Extra;

        [HideInInspector]
        public bool Handled;

        private Texture GetStatusTexture()
        {
            var icon = EditorGUIUtility.IconContent(Handled ? "TestPassed" : "TestFailed");
            return icon != null ? icon.image : null;
        }
    }
    private void AddAsset(string relativePath)
    {
        // Load the asset
        var obj = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
        if (obj == null)
        {
            Debug.LogError("Cannot load asset at: " + relativePath);
            return;
        }

        AddAsset(obj);
    }
    private void AddAsset(UnityEngine.Object obj)
    {
        if (obj == null) return;

        var relativePath = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(relativePath))
        {
            EditorUtility.DisplayDialog("Invalid Object", "The provided object does not have a valid asset path.", "OK");
            return;
        }

        // Load the object (ensure main object)
        var main = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
        if (main == null)
        {
            EditorUtility.DisplayDialog("Load Failed", "Cannot load asset at: " + relativePath, "OK");
            return;
        }

        if (stringItems == null) stringItems = new List<StringItem>();
        if (stringItems.Exists(it => it != null && string.Equals(it.AssetPath, relativePath, StringComparison.OrdinalIgnoreCase)))
        {
            // EditorUtility.DisplayDialog("Duplicate", "This asset is already in the list:\n" + relativePath, "OK");
            return;
        }


        string folderName = UnderWhichFolder(relativePath, out bool showItem);
        if(!showItem) return;
        
        stringItems.Add(new StringItem
        {
            Asset = main,
            AssetPath = relativePath,
            NeedMove = false,
            UnderFolder = folderName
        });

        Repaint();
    }

    private string UnderWhichFolder(string assetPath, out bool showItem)
    {
        foreach (ReportPathItem pathItem in ReportPaths)
        {
            if(!pathItem.JoinCheck && !pathItem.NotShow) continue;
            
            // 判断 assetPath 是否位于 pathItem.Path 所代表的文件夹下（仅父文件夹判断）
            if (pathItem == null) continue;
            var folderPath = pathItem.Path;
            if (string.IsNullOrEmpty(folderPath)) continue;

            // 归一化分隔符
            folderPath = folderPath.Replace("\\", "/");
            assetPath = string.IsNullOrEmpty(assetPath) ? string.Empty : assetPath.Replace("\\", "/");

            string folderRel;
            
            // 判断是相对路径（以Assets开头）还是绝对路径
            if (folderPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                // 已经是相对路径，直接使用
                folderRel = folderPath;
            }
            else
            {
                // 是绝对路径，需要转换为相对路径
                var assetsRoot = Application.dataPath.Replace("\\", "/");
                if (!folderPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
                {
                    // 选中的文件夹不在项目 Assets 下，跳过
                    continue;
                }
                folderRel = "Assets" + folderPath.Substring(assetsRoot.Length);
            }

            // 确保以斜杠结尾，避免类似 Assets/Foo 与 Assets/Foobar 的前缀误判
            if (!folderRel.EndsWith("/")) folderRel += "/";

            // 同样规范化 assetPath，不允许空
            if (string.IsNullOrEmpty(assetPath)) continue;
            
            // 检查资源路径是否匹配
            bool isMatch = false;
            
            if (pathItem.IncludeChildFolders)
            {
                // 包含子文件夹：检查资源路径是否以目标文件夹路径开头
                isMatch = assetPath.StartsWith(folderRel, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // 不包含子文件夹：检查资源是否直接在目标文件夹下
                // 获取资源所在的目录路径
                string assetDir = System.IO.Path.GetDirectoryName(assetPath).Replace("\\", "/");
                if (!assetDir.EndsWith("/")) assetDir += "/";
                
                // 检查资源目录是否等于目标文件夹路径
                isMatch = string.Equals(assetDir, folderRel, StringComparison.OrdinalIgnoreCase);
            }
            
            if (isMatch)
            {
                // 提取文件夹名称（去除末尾斜杠，获取最后一个路径段）
                var folderName = folderRel.TrimEnd('/');
                var lastSlashIndex = folderName.LastIndexOf('/');
                if (lastSlashIndex >= 0 && lastSlashIndex < folderName.Length - 1)
                {
                    if (pathItem.NotShow)
                    {
                        showItem = false;
                        return string.Empty;
                    }
                    
                    showItem = true;
                    return folderName.Substring(lastSlashIndex + 1);
                }
                if (pathItem.NotShow)
                {
                    showItem = false;
                    return string.Empty;
                }
                showItem = true;
                return folderName;
            }
        }
        showItem = true;
        return string.Empty;
    }

    [Button("BatchMoveAssets"), PropertyOrder(30)]
    private void BatchMoveAssets()
    {
        // 检查游戏是否正在运行
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Operation Not Allowed", "Cannot move assets while the game is running. Please stop the game first.", "OK");
            return;
        }

        // 弹出路径选择窗口
        var selectedPath = EditorUtility.OpenFolderPanel("Select Target Folder", Application.dataPath, "");
        if (string.IsNullOrEmpty(selectedPath))
        {
            return;
        }

        // 将绝对路径转换为 Unity 相对路径
        var assetsRoot = Application.dataPath.Replace("\\", "/");
        selectedPath = selectedPath.Replace("\\", "/");
        
        if (!selectedPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Invalid Path", "The selected folder must be within the project Assets folder.", "OK");
            return;
        }

        var targetFolder = "Assets" + selectedPath.Substring(assetsRoot.Length);
        
        // 确保目标路径以斜杠结尾
        if (!targetFolder.EndsWith("/"))
        {
            targetFolder += "/";
        }

        // 收集需要移动的资源
        var assetsToMove = new List<StringItem>();
        foreach (var item in stringItems)
        {
            if (item != null && item.NeedMove && !string.IsNullOrEmpty(item.AssetPath))
            {
                assetsToMove.Add(item);
            }
        }

        if (assetsToMove.Count == 0)
        {
            EditorUtility.DisplayDialog("No Assets", "No assets with NeedMove = true found.", "OK");
            return;
        }

        // 执行移动操作
        int successCount = 0;
        int failCount = 0;
        var failedAssets = new List<string>();

        foreach (var item in assetsToMove)
        {
            var sourcePath = item.AssetPath;
            var fileName = System.IO.Path.GetFileName(sourcePath);
            var destinationPath = targetFolder + fileName;

            // 检查目标路径是否已存在
            if (AssetDatabase.LoadAssetAtPath<Object>(destinationPath) != null)
            {
                failedAssets.Add($"{sourcePath} -> {destinationPath} (目标已存在)");
                failCount++;
                continue;
            }

            // 移动资源
            var moveResult = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (string.IsNullOrEmpty(moveResult))
            {
                // 移动成功，更新 AssetPath
                item.AssetPath = destinationPath;
                item.NeedMove = false;
                successCount++;
            }
            else
            {
                failedAssets.Add($"{sourcePath} -> {moveResult}");
                failCount++;
            }
        }

        // 刷新资源数据库
        AssetDatabase.Refresh();

        // 显示结果
        if (failCount == 0)
        {
            EditorUtility.DisplayDialog("Move Complete", $"Successfully moved {successCount} asset(s) to {targetFolder}", "OK");
        }
        else
        {
            var failedMessage = $"Moved {successCount} asset(s), failed {failCount} asset(s):\n\n";
            failedMessage += string.Join("\n", failedAssets.Take(10));
            if (failedAssets.Count > 10)
            {
                failedMessage += $"\n... and {failedAssets.Count - 10} more";
            }
            EditorUtility.DisplayDialog("Move Complete (with errors)", failedMessage, "OK");
        }

        RemoveNotShowItems();

        UpdateStringItems();
        
        // 刷新窗口
        Repaint();
    }

    private void RemoveNotShowItems()
    {
        if (stringItems == null || stringItems.Count == 0) return;
        if (ReportPaths == null || ReportPaths.Count == 0) return;

        // 从后往前遍历，这样删除时不会影响索引
        for (int i = stringItems.Count - 1; i >= 0; i--)
        {
            var item = stringItems[i];
            if (item == null || string.IsNullOrEmpty(item.AssetPath)) continue;

            // 使用 UnderWhichFolder 检查该资源是否应该被隐藏
            UnderWhichFolder(item.AssetPath, out bool showItem);
            if (!showItem)
            {
                // 如果 showItem 为 false，说明该资源被 NotShow 影响了，需要移除
                stringItems.RemoveAt(i);
            }
        }
    }

    private void UpdateStringItems()
    {
        foreach (StringItem item in stringItems)
        {
            item.NeedMove = false;
            
            // 刷新 UnderFolder，因为资源路径可能已经变更
            if (!string.IsNullOrEmpty(item.AssetPath))
            {
                string folderName = UnderWhichFolder(item.AssetPath, out bool showItem);
                item.UnderFolder = folderName;
            }
            else
            {
                item.UnderFolder = string.Empty;
            }
        }
    }

    private void SaveReportPaths()
    {
        try
        {
            var data = new ReportPathsData { paths = ReportPaths };
            string json = JsonUtility.ToJson(data, true);
            EditorPrefs.SetString(ReportPathsPrefsKey, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save ReportPaths: {e.Message}");
        }
    }

    private void LoadReportPaths()
    {
        try
        {
            isLoadingReportPaths = true;
            if (EditorPrefs.HasKey(ReportPathsPrefsKey))
            {
                string json = EditorPrefs.GetString(ReportPathsPrefsKey);
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<ReportPathsData>(json);
                    if (data != null && data.paths != null)
                    {
                        ReportPaths = data.paths;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load ReportPaths: {e.Message}");
            ReportPaths = new List<ReportPathItem>();
        }
        finally
        {
            isLoadingReportPaths = false;
        }
    }

    private void OnReportPathsChanged()
    {
        // 如果正在加载，不触发保存
        if (isLoadingReportPaths) return;
        
        SaveReportPaths();
    }

    private void SaveMessengerPath()
    {
        try
        {
            if (messenger != null)
            {
                string path = AssetDatabase.GetAssetPath(messenger);
                if (!string.IsNullOrEmpty(path))
                {
                    EditorPrefs.SetString(MessengerPathPrefsKey, path);
                }
                else
                {
                    // 如果无法获取路径，清除保存的路径
                    EditorPrefs.DeleteKey(MessengerPathPrefsKey);
                }
            }
            else
            {
                // 如果 messenger 为 null，清除保存的路径
                EditorPrefs.DeleteKey(MessengerPathPrefsKey);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save Messenger path: {e.Message}");
        }
    }

    private string LoadMessengerPath()
    {
        try
        {
            if (EditorPrefs.HasKey(MessengerPathPrefsKey))
            {
                string path = EditorPrefs.GetString(MessengerPathPrefsKey);
                if (!string.IsNullOrEmpty(path))
                {
                    // 验证路径是否仍然有效
                    var asset = AssetDatabase.LoadAssetAtPath<DyeingSo>(path);
                    if (asset != null)
                    {
                        return path;
                    }
                    else
                    {
                        // 路径无效，清除保存的路径
                        EditorPrefs.DeleteKey(MessengerPathPrefsKey);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load Messenger path: {e.Message}");
        }
        return string.Empty;
    }

    private string GetDyeingSoPath()
    {
        // 使用 AssetDatabase 查找当前脚本的路径
        var scriptType = typeof(AssetDyeingWindow);
        var guids = AssetDatabase.FindAssets($"t:MonoScript {scriptType.Name}");
        
        foreach (var guid in guids)
        {
            var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(scriptPath)) continue;
            
            // 检查是否是当前脚本（通过文件名匹配）
            if (!scriptPath.EndsWith($"{scriptType.Name}.cs", StringComparison.OrdinalIgnoreCase))
                continue;
            
            // 获取脚本所在目录（EditorWindow 文件夹）
            string scriptDir = System.IO.Path.GetDirectoryName(scriptPath).Replace("\\", "/");
            
            // 获取上级目录（EditorWindow 的上级）
            string parentDir = System.IO.Path.GetDirectoryName(scriptDir).Replace("\\", "/");
            
            // 进入 So 文件夹
            string soFolder = $"{parentDir}/So";
            
            // 构建 DyeingSo.asset 路径
            string dyeingSoPath = $"{soFolder}/DyeingSo.asset";
            
            return dyeingSoPath;
        }
        
        return string.Empty;
    }
}