using UnityEditor;

using SW.Base;

namespace SW.Editor.Base
{
    /// <summary>
    /// SWScriptableObject를 상속받은 모든 에셋에 SWUtils 인스펙터를 적용합니다.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SWScriptableObject), true)]
    public class SWScriptableObjectEditor : SWMonoBehaviourEditor
    {
    }
}
