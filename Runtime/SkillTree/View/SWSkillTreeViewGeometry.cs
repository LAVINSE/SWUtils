using UnityEngine;

namespace SW.SkillTree
{
    /// <summary>화면 갱신 시점이나 Canvas 확대에 영향받지 않는 공통 부모 기준 좌표를 계산합니다.</summary>
    internal static class SWSkillTreeViewGeometry
    {
        /// <summary>두 요소의 공통 부모까지 로컬 변환을 합성하여 상대 변환을 반환합니다.</summary>
        internal static Matrix4x4 GetRelativeMatrix(Transform source, Transform reference)
        {
            Transform common = source;
            while (common != null && !reference.IsChildOf(common)) common = common.parent;
            return ToAncestor(reference, common).inverse * ToAncestor(source, common);
        }

        private static Matrix4x4 ToAncestor(Transform source, Transform ancestor)
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            for (Transform current = source; current != ancestor && current != null; current = current.parent)
                matrix = Matrix4x4.TRS(current.localPosition, current.localRotation, current.localScale) * matrix;
            return matrix;
        }
    }
}
