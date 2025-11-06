using UnityEngine;

public class RuntimeSender : MonoBehaviour
{
    // 在 Inspector 中拖入创建的 ScriptableObject 资产
    public DyeingSo messenger;
    
    private int _counter = 0;

    void Update()
    {
        // 假设每隔 100 帧发送一次消息
        if (Time.frameCount % 1000 == 0)
        {
            string assetPath1 = "Assets/DemoResources/TestAssets/Arts/test.jpg";
            // string assetPath2 = "Assets/DemoResources/TestAssets/UIs/ImageTest.prefab";
            DyeingObj obj = new DyeingObj();
            obj.AssetPath = assetPath1;
            // string message = JsonUtility.ToJson(obj);
            
            // 发送消息
            if (messenger != null)
            {
                messenger.SendMessageToEditor(obj);
            }
        }
    }
}