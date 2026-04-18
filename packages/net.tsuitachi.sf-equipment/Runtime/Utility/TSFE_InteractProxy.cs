using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TSFE.Utility
{
    /// <summary>
    /// Interact時に別GameObjectのメソッドを呼び出すプロキシコンポーネント
    /// TriggerCollider配下に配置し、SFEXT_Chockなど他のコンポーネントのメソッドを転送する
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TSFE_InteractProxy : UdonSharpBehaviour
    {
        [Header("Target")]
        [Tooltip("メソッドを呼び出す対象のUdonBehaviour")]
        public UdonSharpBehaviour targetBehaviour;

        [Tooltip("呼び出すメソッド名")]
        public string methodName = "Interact";

        [Header("Debug")]
        [Tooltip("デバッグモード: Interact時にログ出力")]
        public bool debugMode = false;

        public override void Interact()
        {
            if (debugMode)
            {
                Debug.Log($"[TSFE_InteractProxy] Interact called on GameObject: {gameObject.name}");
                Debug.Log($"[TSFE_InteractProxy] Target: {(targetBehaviour != null ? targetBehaviour.GetType().Name : "null")}, Method: {methodName}");
            }

            if (targetBehaviour != null && !string.IsNullOrEmpty(methodName))
            {
                targetBehaviour.SendCustomEvent(methodName);

                if (debugMode)
                {
                    Debug.Log($"[TSFE_InteractProxy] SendCustomEvent(\"{methodName}\") completed successfully");
                }
            }
            else
            {
                if (debugMode)
                {
                    if (targetBehaviour == null)
                    {
                        Debug.LogWarning($"[TSFE_InteractProxy] Target behaviour is null on {gameObject.name}");
                    }
                    if (string.IsNullOrEmpty(methodName))
                    {
                        Debug.LogWarning($"[TSFE_InteractProxy] Method name is empty on {gameObject.name}");
                    }
                }
            }
        }
    }
}
