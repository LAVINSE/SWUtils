using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SW.Util;

namespace SW.EditorTools.Util
{
    /// <summary>
    /// SWRectDummy 인스펙터와 생성 메뉴입니다.
    /// </summary>
    [CustomEditor(typeof(SWRectDummy))]
    [CanEditMultipleObjects]
    public class SWRectDummyEditor : UnityEditor.Editor
    {
        #region 필드
        private const double MenuDuplicateGuardSeconds = 0.1d;
        private static DateTime lastMenuTime = DateTime.MinValue;
        #endregion // 필드

        #region 메뉴
        /// <summary>
        /// 선택한 UI 오브젝트 아래에 SWRectDummy를 생성합니다.
        /// </summary>
        [MenuItem("GameObject/UI/SW Rect Dummy", false, 10000)]
        public static void AddDummy()
        {
            if ((DateTime.Now - lastMenuTime).TotalSeconds < MenuDuplicateGuardSeconds) return;
            lastMenuTime = DateTime.Now;

            GameObject[] selectedObjects = Selection.gameObjects;
            var createdObjects = new List<UnityEngine.Object>(selectedObjects.Length);

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                GameObject selectedObject = selectedObjects[i];
                if (selectedObject == null) continue;

                var dummyObject = new GameObject("SW Rect Dummy", typeof(RectTransform), typeof(SWRectDummy));
                Undo.RegisterCreatedObjectUndo(dummyObject, "Create SW Rect Dummy");
                dummyObject.transform.SetParent(selectedObject.transform, false);

                var dummy = dummyObject.GetComponent<SWRectDummy>();
                dummy.FitParent();
                createdObjects.Add(dummyObject);
            }

            if (createdObjects.Count > 0)
            {
                Selection.objects = createdObjects.ToArray();
            }
        }
        #endregion // 메뉴

        #region 인스펙터
        /// <summary>
        /// SWRectDummy 인스펙터를 그립니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty raycastTargetProperty = serializedObject.FindProperty("m_RaycastTarget");
            EditorGUILayout.PropertyField(raycastTargetProperty);

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fit Parent"))
                {
                    FitSelectedParents();
                }

                if (GUILayout.Button("Raycast Off Children"))
                {
                    RaycastOffChildrenOfHandler();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void FitSelectedParents()
        {
            foreach (UnityEngine.Object targetObject in targets)
            {
                var dummy = targetObject as SWRectDummy;
                if (dummy == null) continue;

                Undo.RecordObject(dummy.rectTransform, "Fit SW Rect Dummy Parent");
                dummy.FitParent();
                EditorUtility.SetDirty(dummy.rectTransform);
            }
        }

        private void RaycastOffChildrenOfHandler()
        {
            foreach (UnityEngine.Object targetObject in targets)
            {
                if (targetObject is SWRectDummy dummy)
                {
                    RaycastOffChildrenOfHandler(dummy);
                }
            }
        }

        private static void RaycastOffChildrenOfHandler(SWRectDummy dummy)
        {
            Component eventHandler = GetEventHandlerInParent(dummy.transform);
            if (eventHandler == null || eventHandler.gameObject == dummy.gameObject) return;
            if (eventHandler.GetComponent<Scrollbar>() != null || eventHandler.GetComponent<ScrollRect>() != null) return;

            var graphicObjects = new List<UnityEngine.Object>();
            Graphic[] graphics = eventHandler.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || graphic is SWRectDummy || !graphic.raycastTarget) continue;
                graphicObjects.Add(graphic);
            }

            Undo.RecordObjects(graphicObjects.ToArray(), "Raycast Off Children");
            for (int i = 0; i < graphicObjects.Count; i++)
            {
                var graphic = graphicObjects[i] as Graphic;
                if (graphic == null) continue;

                graphic.raycastTarget = false;
                EditorUtility.SetDirty(graphic);
            }
        }

        private static Component GetEventHandlerInParent(Transform startTransform)
        {
            Transform currentTransform = startTransform;
            while (currentTransform != null)
            {
                Component[] components = currentTransform.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component is IEventSystemHandler)
                    {
                        return component;
                    }
                }

                currentTransform = currentTransform.parent;
            }

            return null;
        }
        #endregion // 인스펙터
    }
}
