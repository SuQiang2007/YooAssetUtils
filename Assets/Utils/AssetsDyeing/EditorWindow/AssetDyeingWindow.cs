using System;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class AssetDyeingWindow : OdinEditorWindow
{
    [MenuItem("YooAssetUtils/Asset Dyeing Window")]
    public static void ShowWindow()
    {
        GetWindow<AssetDyeingWindow>("Asset Dyeing");
    }

    [Title("Settings")]
    [InfoBox("Assign the DyeingSo asset if needed. Below you can manage .report paths and the display list.", InfoMessageType.Info)]
    [AssetSelector]
    [OnValueChanged(nameof(OnMessengerChanged), true)]
    [LabelText("Messenger Asset")]
    public DyeingSo Messenger;

	[Title("Report Paths"), PropertyOrder(10)]
	[ListDrawerSettings(
		DraggableItems = false,
		Expanded = true,
		ShowIndexLabels = false,
		CustomAddFunction = nameof(AddReportPath),
		CustomRemoveIndexFunction = nameof(RemoveReportPath),
		OnEndListElementGUI = nameof(AfterReportPathElementGUI))]
	[LabelText("Paths (.*.report)")]
	public List<ReportPathItem> ReportPaths = new List<ReportPathItem>();

    [Button("Add Folder"), PropertyOrder(11)]
    private void AddReportPathViaPicker()
    {
        AddReportPath();
    }
    
    [TableList(
         ShowIndexLabels = false
         // IsReadOnly = true
         ), PropertyOrder(20)]
    public List<StringItem> stringItems = new List<StringItem>();

    [System.Serializable]
    public class StringItem
    {
        public StringItem()
        {
            
        }
        [LabelText("AssetPath")]
        [TableColumnWidth(100)]
        [ReadOnly]
        public UnityEngine.Object Aasset;
        
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

    private void OnEnable()
    {
        if (Messenger == null)
        {
            Messenger = AssetDatabase.LoadAssetAtPath<DyeingSo>("Assets/Utils/AssetsDyeing/DyeingSo.asset");
        }
        TrySubscribe(Messenger);

        var aa = new ReportPathItem();
        aa.Path = "Assets/DemoResources/TestAssets/UIs";
        aa.JoinCheck = true;
        aa.NotShow = false;
        ReportPaths.Add(aa);

        string assetPath1 = "Assets/DemoResources/TestAssets/Arts/test.jpg";
        string assetPath2 = "Assets/DemoResources/TestAssets/UIs/ImageTest.prefab";
        AddAsset(assetPath1);
        var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath2);
        AddAsset(obj);
    }

    private void OnDisable()
    {
        TryUnsubscribe(Messenger);
    }

    private void OnMessengerChanged()
    {
        // Resubscribe when the Messenger reference changes
        foreach (var so in new List<DyeingSo>(subscribed))
        {
            TryUnsubscribe(so);
        }
        TrySubscribe(Messenger);
        Repaint();
    }

    private void TrySubscribe(DyeingSo so)
    {
        if (so == null) return;
        if (subscribed.Contains(so)) return;
        so.OnMessageSent += HandleMessage;
        subscribed.Add(so);
    }

    private void TryUnsubscribe(DyeingSo so)
    {
        if (so == null) return;
        if (!subscribed.Contains(so)) return;
        so.OnMessageSent -= HandleMessage;
        subscribed.Remove(so);
    }

    private void HandleMessage(string message)
    {
        // If needed, you can react to messenger events here (e.g., add to Items)
        Repaint();
    }

    // ===== Helpers for ReportPaths =====
	private void AddReportPath()
    {
        var path = EditorUtility.OpenFolderPanel("Select folder", Application.dataPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            // if (!path.EndsWith(".report"))
            // {
            //     EditorUtility.DisplayDialog("Invalid File", "Please select a file with .report suffix.", "OK");
            //     return;
            // }
			if (!ReportPaths.Exists(p => p != null && p.Path == path))
            {
				ReportPaths.Add(new ReportPathItem { Path = path });
            }
        }
    }

	private void RemoveReportPath(int index)
    {
        if (index >= 0 && index < ReportPaths.Count)
        {
            ReportPaths.RemoveAt(index);
        }
    }

	[System.Serializable]
	public class ReportPathItem
	{
		[HorizontalGroup("rp")]
		[Sirenix.OdinInspector.FilePath(AbsolutePath = true)]
		[HideLabel]
		public string Path;

		[HorizontalGroup("rp", Width = 0.2f)]
		[LabelText("JoinCheck")]
		public bool JoinCheck;

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
            EditorUtility.DisplayDialog("Load Failed", "Cannot load asset at: " + relativePath, "OK");
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


        string aaa = UnderWhichFolder(relativePath);
        
        // var folder = System.IO.Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? string.Empty;

        stringItems.Add(new StringItem
        {
            Aasset = main,
            AssetPath = relativePath,
            NeedMove = false,
            UnderFolder = aaa
        });

        Repaint();
    }

    private string UnderWhichFolder(string assetPath)
    {
        foreach (ReportPathItem pathItem in ReportPaths)
        {
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
            // 允许大小写不敏感比较（Windows）
            if (assetPath.StartsWith(folderRel, StringComparison.OrdinalIgnoreCase))
            {
                // 提取文件夹名称（去除末尾斜杠，获取最后一个路径段）
                var folderName = folderRel.TrimEnd('/');
                var lastSlashIndex = folderName.LastIndexOf('/');
                if (lastSlashIndex >= 0 && lastSlashIndex < folderName.Length - 1)
                {
                    return folderName.Substring(lastSlashIndex + 1);
                }
                return folderName;
            }
        }
        return string.Empty;
    }
}