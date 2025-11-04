using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;
using AssetInfo = YooAsset.Editor.AssetInfo;
using Object = UnityEngine.Object;
using System.Text;

public class PackageDependencyChecker : EditorWindow
{
    private List<string> reportPaths = new List<string>();
    private string outputFilePath = string.Empty;

    [MenuItem("YooAssetUtils/Duplicate Checker")]
    static void ShowWindow()
    {
        var window = GetWindow<PackageDependencyChecker>("YooAsset Duplicate Checker");
        window.minSize = new Vector2(700, 500);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("选择多个 BuildReport.report 文件，用于检测跨 Package 的重复资源，并将结果写入指定的 txt 文件。", MessageType.Info);

        // 文件选择区
        EditorGUILayout.Space(5);
        if (GUILayout.Button("添加 BuildReport.report 文件"))
        {
            string path = EditorUtility.OpenFilePanel("选择 BuildReport.report", "", "report");
            if (!string.IsNullOrEmpty(path) && !reportPaths.Contains(path))
            {
                reportPaths.Add(path);
            }
        }

        // 显示已添加的路径
        for (int i = 0; i < reportPaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i + 1}. {Path.GetFileName(reportPaths[i])}", GUILayout.Width(300));
            EditorGUILayout.LabelField(reportPaths[i]);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                reportPaths.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        // 输出文件选择
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("输出文件 (txt)");
        EditorGUILayout.BeginHorizontal();
        outputFilePath = EditorGUILayout.TextField(outputFilePath);
        if (GUILayout.Button("浏览...", GUILayout.Width(80)))
        {
            string initialName = string.IsNullOrEmpty(Path.GetFileName(outputFilePath))
                ? "duplicate_assets.txt"
                : Path.GetFileName(outputFilePath);
            string initialDir = string.IsNullOrEmpty(outputFilePath)
                ? Application.dataPath
                : Path.GetDirectoryName(outputFilePath);
            string savePath = EditorUtility.SaveFilePanel("选择输出文件", initialDir, initialName, "txt");
            if (!string.IsNullOrEmpty(savePath))
            {
                outputFilePath = savePath;
            }
        }

        EditorGUILayout.EndHorizontal();

        // 执行按钮
        if (reportPaths.Count >= 2)
        {
            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(outputFilePath)))
            {
                if (GUILayout.Button("开始检测并写入文件", GUILayout.Height(30)))
                {
                    if (string.IsNullOrWhiteSpace(outputFilePath))
                    {
                        EditorUtility.DisplayDialog("输出路径为空", "请先指定输出文件路径。", "确定");
                        return;
                    }

                    CheckDuplicatesAndWrite(outputFilePath);
                }
            }

            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                EditorGUILayout.HelpBox("请先指定输出的 txt 文件路径。", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请至少添加两个 BuildReport.report 文件。", MessageType.Warning);
        }
    }

    private List<BuildReport> _reports = new();

    void CheckDuplicatesAndWrite(string outPath)
    {
        StringBuilder sb = new();
        sb.AppendLine("YooAsset Duplicate Report");
        sb.AppendLine($"GeneratedAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"ReportsUsed: {reportPaths.Count}");
        sb.AppendLine();
        sb.AppendLine("AssetPath | Count");
        sb.AppendLine("-----------------");

        int duplicateCount = 0;
        _reports = InitBuildReports();
        foreach (var report in _reports)
        {
            if (report?.AssetInfos == null)
                continue;

            foreach (ReportAssetInfo asset in report.AssetInfos)
            {
                if (string.IsNullOrEmpty(asset.AssetPath))
                    continue;

                foreach (AssetInfo dependAsset in asset.DependAssets)
                {
                    foreach (BuildReport otherReport in _reports)
                    {
                        if (report == otherReport) continue;

                        if (InPackage(dependAsset.AssetPath, otherReport))
                        {
                            duplicateCount++;
                            sb.AppendLine(
                                $"检测到{report.Summary.BuildPackageName}中的\n      {asset.AssetPath}依赖的{dependAsset.AssetPath}\n            存在于{otherReport.Summary.BuildPackageName}中！");
                        }

                    }
                }
            }
        }

        string dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        sb.AppendLine($"检测完成，重复资源：{duplicateCount} 项，已写入：{outPath}");
        File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"检测完成，重复资源：{duplicateCount} 项，已写入：{outPath}");
        EditorUtility.RevealInFinder(outPath);
    }

    private List<BuildReport> InitBuildReports()
    {
        List<BuildReport> reportsResult = new List<BuildReport>();
        foreach (string reportPath in reportPaths)
        {
            string jsonData = ReadAllText(reportPath);
            BuildReport report = BuildReport.Deserialize(jsonData);

            reportsResult.Add(report);
        }

        return reportsResult;
    }

    private string ReadAllText(string filePath)
    {
        if (File.Exists(filePath) == false)
            return null;
        return File.ReadAllText(filePath, Encoding.UTF8);
    }

    private bool InPackage(string assetPath, BuildReport report)
    {
        foreach (ReportAssetInfo reportAssetInfo in report.AssetInfos)
        {
            if (assetPath == reportAssetInfo.AssetPath)
            {
                return true;
            }
        }

        return false;
    }
}