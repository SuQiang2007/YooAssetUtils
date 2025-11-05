using UnityEngine;

public class RuntimeSender : MonoBehaviour
{
    // 在 Inspector 中拖入创建的 ScriptableObject 资产
    public DyeingSo messenger;
    
    private int _counter = 0;

    void Update()
    {
        // 假设每隔 100 帧发送一次消息
        if (Time.frameCount % 100 == 0)
        {
            string message = "Game Time: " + Time.time.ToString("F2") + ", Count: " + _counter++;
            
            // 发送消息
            if (messenger != null)
            {
                messenger.SendMessageToEditor(message);
            }
        }
    }
}