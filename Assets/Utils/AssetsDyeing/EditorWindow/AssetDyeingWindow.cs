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

    [Button("Add .report File"), PropertyOrder(11)]
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
        var path = EditorUtility.OpenFilePanel("Select .report file", Application.dataPath, "report");
        if (!string.IsNullOrEmpty(path))
        {
            if (!path.EndsWith(".report"))
            {
                EditorUtility.DisplayDialog("Invalid File", "Please select a file with .report suffix.", "OK");
                return;
            }
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
		[Sirenix.OdinInspector.FilePath(Extensions = "report", AbsolutePath = true)]
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

    // Legacy custom drawer removed; TableList handles scrolling

    private static GUIContent s_CheckIcon;
    private static GUIContent s_CrossIcon;

    private void DrawPathLink(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            GUILayout.Label("<empty>", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            return;
        }

        var style = new GUIStyle(EditorStyles.linkLabel);
        if (GUILayout.Button(path, style, GUILayout.ExpandWidth(true)))
        {
            PingPath(path);
        }
    }

    private static void PingPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return;
        var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (obj != null)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
        else
        {
            EditorUtility.DisplayDialog("Asset Not Found", "Cannot load asset at: " + assetPath, "OK");
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

        // Prevent duplicates by AssetPath
        if (stringItems == null) stringItems = new List<StringItem>();
        if (stringItems.Exists(it => it != null && string.Equals(it.AssetPath, relativePath, StringComparison.OrdinalIgnoreCase)))
        {
            EditorUtility.DisplayDialog("Duplicate", "This asset is already in the list:\n" + relativePath, "OK");
            return;
        }

        // Compute folder path for UnderFolder
        var folder = System.IO.Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? string.Empty;

        // Add item
        stringItems.Add(new StringItem
        {
            Aasset = obj,
            AssetPath = relativePath,
            NeedMove = false,
            UnderFolder = folder
        });

        Repaint();
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
            EditorUtility.DisplayDialog("Duplicate", "This asset is already in the list:\n" + relativePath, "OK");
            return;
        }

        var folder = System.IO.Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? string.Empty;

        stringItems.Add(new StringItem
        {
            Aasset = main,
            AssetPath = relativePath,
            NeedMove = false,
            UnderFolder = folder
        });

        Repaint();
    }
}