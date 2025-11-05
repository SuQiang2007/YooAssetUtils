using YooAsset.Editor;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class BuildPipelineController
{
    [MenuItem("YooAsset/构建/自定义打包")]
    public static void BuildWithCustomGroups()
    {
        string packageName = "Test";
        // 第一步：设置分组状态，Group是否参与本次打包
        // var groupStates = new List<GroupEnableSet>();
        // groupStates.Add(new GroupEnableSet(packageName, "effect", false));
        // groupStates.Add(new GroupEnableSet(packageName, "animation", false));
        // groupStates.Add(new GroupEnableSet(packageName, "audio", false));
        // groupStates.Add(new GroupEnableSet(packageName, "behaviac", false));
        // groupStates.Add(new GroupEnableSet(packageName, "config", false));
        // groupStates.Add(new GroupEnableSet(packageName, "default", false));
        // groupStates.Add(new GroupEnableSet(packageName, "materials", false));
        // groupStates.Add(new GroupEnableSet(packageName, "models", false));
        // groupStates.Add(new GroupEnableSet(packageName, "raw", false));
        // groupStates.Add(new GroupEnableSet(packageName, "room", false));
        // groupStates.Add(new GroupEnableSet(packageName, "scene", false));
        // groupStates.Add(new GroupEnableSet(packageName, "scenebgsprite", false));
        // groupStates.Add(new GroupEnableSet(packageName, "shader", false));
        // groupStates.Add(new GroupEnableSet(packageName, "shadereffect", false));
        // groupStates.Add(new GroupEnableSet(packageName, "shadertexture", false));
        // groupStates.Add(new GroupEnableSet(packageName, "stuff", false));
        // groupStates.Add(new GroupEnableSet(packageName, "ui", true));
        // groupStates.Add(new GroupEnableSet(packageName, "ui2", true));
        //
        // //应用组设置到ScriptableObject设置，并保存！
        // BuildHelper.SetGroupsEnableState(groupStates);
        
        // 第二步：执行打包
        BuildInternal(packageName, "xxx");
        
        //第三步：如果打包，则会调用PreprocessBuildCatalog的预处理流程，Copy文件到StreamingAssets
    }
    
    private static void BuildInternal(string packageName, string tag)
    {
        
        ScriptableBuildParameters buildParameters = new ScriptableBuildParameters
        {
            BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot(),
            BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
            BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString(),
            BuildBundleType = (int)EBuildBundleType.AssetBundle,
            BuildTarget = EditorUserBuildSettings.activeBuildTarget,
            PackageName = packageName,
            PackageVersion = GetVersionString(),
            EnableSharePackRule = true,
            VerifyBuildingResult = true,
            FileNameStyle = EFileNameStyle.BundleName,
            BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
            BuildinFileCopyParams = tag,//首包资源Tag，类似："UI,Config,Base,Shader"
            CompressOption = ECompressOption.Uncompressed,
            ClearBuildCacheFiles = false,
            UseAssetDependencyDB = true,
            EncryptionServices = null,//传入IEncryptionServices的实现类
            ManifestProcessServices = null,//IManifestProcessServices的实现类
            ManifestRestoreServices = null,//IManifestRestoreServices的实现类
            BuiltinShadersBundleName = GetBuiltinShaderBundleName(packageName),
        };

        ScriptableBuildPipeline pipeline = new ScriptableBuildPipeline();
        var buildResult = pipeline.Run(buildParameters, true);
        if (buildResult.Success)
            EditorUtility.RevealInFinder(buildResult.OutputPackageDirectory);
    }
    
    
    /// <summary>
    /// 内置着色器资源包名称
    /// 注意：和自动收集的着色器资源包名保持一致！
    /// </summary>
    private static string GetBuiltinShaderBundleName(string packageName)
    {
        var uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
        var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
        return packRuleResult.GetBundleName(packageName, uniqueBundleName);
    }
    //2025-10-27-1245
    private static string GetVersionString()
    {
        // 生成一个2025-10-27-1245格式的版本号
        return System.DateTime.Now.ToString("yyyy-MM-dd-HHmm");
    }
}