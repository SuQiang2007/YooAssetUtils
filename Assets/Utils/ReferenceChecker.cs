using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class ReferenceChecker : EditorWindow
{
    private Object _targetAsset;
    private string _searchFolder = "Assets";
    private readonly List<string> _dependentAssetPaths = new List<string>();
    private Vector2 _scrollPos;

    [MenuItem("YooAssetUtils/Reference Checker")]
    private static void Open()
    {
        var window = GetWindow<ReferenceChecker>(true, "Reference Checker", true);
        window.minSize = new Vector2(520, 360);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("检查被哪些资源依赖", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            // 目标资源选择
            EditorGUILayout.LabelField("目标资源 (被依赖者)");
            _targetAsset = EditorGUILayout.ObjectField(_targetAsset, typeof(Object), false);

            EditorGUILayout.Space();

            // 路径选择
            EditorGUILayout.LabelField("在此路径中查找 (相对 Assets)");
            using (new EditorGUILayout.HorizontalScope())
            {
                _searchFolder = EditorGUILayout.TextField(_searchFolder);
                if (GUILayout.Button("选择路径", GUILayout.Width(90)))
                {
                    var abs = EditorUtility.OpenFolderPanel("选择要检查的路径", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(abs))
                    {
                        // 转为以 Assets 开头的相对路径
                        var dataPath = Application.dataPath.Replace('\\', '/');
                        var normalized = abs.Replace('\\', '/');
                        if (normalized.StartsWith(dataPath))
                        {
                            _searchFolder = "Assets" + normalized.Substring(dataPath.Length);
                        }
                        else
                        {
                            // 非工程内路径，回退为 Assets
                            _searchFolder = "Assets";
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("开始检查", GUILayout.Height(28)))
                {
                    RunSearch();
                }
                if (GUILayout.Button("清空结果", GUILayout.Height(28), GUILayout.Width(90)))
                {
                    _dependentAssetPaths.Clear();
                }
            }
        }

        EditorGUILayout.Space();

        // 结果展示
        EditorGUILayout.LabelField("依赖该资源的项目 (可点击导航)", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            if (_dependentAssetPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无结果。请选择目标资源并点击开始检查。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"共 {_dependentAssetPaths.Count} 个结果");
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                for (int i = 0; i < _dependentAssetPaths.Count; i++)
                {
                    var path = _dependentAssetPaths[i];
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // 显示对象并可点击选中
                        GUI.enabled = obj != null;
                        if (GUILayout.Button("选中", GUILayout.Width(60)))
                        {
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                        }
                        if (GUILayout.Button("定位", GUILayout.Width(60)))
                        {
                            if (obj != null)
                            {
                                EditorGUIUtility.PingObject(obj);
                            }
                        }
                        GUI.enabled = true;

                        EditorGUILayout.ObjectField(obj, typeof(Object), false);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }

    private void RunSearch()
    {
        _dependentAssetPaths.Clear();

        if (_targetAsset == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选择目标资源。", "确定");
            return;
        }

        var targetPath = AssetDatabase.GetAssetPath(_targetAsset);
        if (string.IsNullOrEmpty(targetPath))
        {
            EditorUtility.DisplayDialog("提示", "无法获取目标资源路径。", "确定");
            return;
        }

        if (string.IsNullOrEmpty(_searchFolder) || !_searchFolder.StartsWith("Assets"))
        {
            _searchFolder = "Assets";
        }

        var searchIn = new[] { _searchFolder };
        var guids = AssetDatabase.FindAssets(string.Empty, searchIn);

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("正在检查依赖", $"{i + 1}/{guids.Length}", (float)(i + 1) / guids.Length))
                {
                    break;
                }

                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }

                if (path == targetPath)
                {
                    continue; // 自身不计
                }

                var deps = AssetDatabase.GetDependencies(path, true);
                for (int d = 0; d < deps.Length; d++)
                {
                    if (deps[d] == targetPath)
                    {
                        _dependentAssetPaths.Add(path);
                        break;
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 排序便于浏览
        _dependentAssetPaths.Sort();
    }
}
#endif
