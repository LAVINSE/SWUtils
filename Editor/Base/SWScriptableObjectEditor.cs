using UnityEditor;

using SW.Base;

namespace SW.EditorTools.Base
{
    /// <summary>
    /// SWScriptableObject를 상속받은 모든 에셋에 SWUtils 인스펙터를 적용합니다.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SWScriptableObject), true)]
    public class SWScriptableObjectEditor : SWMonoBehaviourEditor
    {
        /// <summary>기존 제작 창에서 직접 그리는 식별 에셋에도 공통 폴드아웃을 적용합니다.</summary>
        public override void OnInspectorGUI()
        {
            if (target is SWIdentifiedObject) DrawGroupedInspector();
            else base.OnInspectorGUI();
        }
    }
}
