using UnityEngine;
using UnityEditor; // 尽管我们在运行时访问，但这是 Unity 的核心命名空间

// 创建资产菜单，方便在 Editor 中创建实例
[CreateAssetMenu(fileName = "DyeingSo", menuName = "YooAssetUtils/DyeingSo")]
public class DyeingSo : ScriptableObject
{
    // 用于存储要传递的数据
    public DyeingObj MessageData;

    // 用于触发事件（运行时发送，Editor 接收）
    public System.Action<DyeingObj> OnMessageSent;

    // 运行时代码调用的方法
    public void SendMessageToEditor(DyeingObj message)
    {
        Debug.Log("Runtime Sending: " + message);
        MessageData = message;
        
        // 触发事件
        if (OnMessageSent != null)
        {
            OnMessageSent.Invoke(message);
        }

        // 重要：在 Editor 中调用此方法，强制 EditorWindow 刷新
        // 仅在 Editor 模式下运行
#if UNITY_EDITOR
        // 告诉 Editor 发生了变化，可以用于触发 EditorWindow 的 Repaint
        EditorUtility.SetDirty(this); 
#endif
    }
}