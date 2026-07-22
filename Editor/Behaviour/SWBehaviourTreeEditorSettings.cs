using UnityEditor;
using UnityEngine;

namespace SW.EditorTools.Behaviour
{
    /// <summary>Behaviour Tree 편집기의 프로젝트 공통 표시와 배치 설정입니다.</summary>
    [FilePath("ProjectSettings/SWBehaviourTreeEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class SWBehaviourTreeEditorSettings : ScriptableSingleton<SWBehaviourTreeEditorSettings>
    {
        [SerializeField] private float nodeWidth = 220f;
        [SerializeField] private float horizontalSpacing = 30f;
        [SerializeField] private float verticalSpacing = 180f;
        [SerializeField] private float blackboardWidth = 300f;
        [SerializeField] private float inspectorWidth = 330f;
        [SerializeField] private float panelHeight = 560f;
        [SerializeField] private bool showDescriptions = true;
        [SerializeField] private int runtimeRefreshMilliseconds = 200;

        public float NodeWidth => nodeWidth;
        public float HorizontalSpacing => horizontalSpacing;
        public float VerticalSpacing => verticalSpacing;
        public float BlackboardWidth { get => blackboardWidth; set => blackboardWidth = value; }
        public float InspectorWidth { get => inspectorWidth; set => inspectorWidth = value; }
        public float PanelHeight { get => panelHeight; set => panelHeight = value; }
        public bool ShowDescriptions => showDescriptions;
        public int RuntimeRefreshMilliseconds => runtimeRefreshMilliseconds;

        public void SaveSettings()
        {
            nodeWidth = Mathf.Clamp(nodeWidth, 180f, 360f);
            horizontalSpacing = Mathf.Clamp(horizontalSpacing, 10f, 120f);
            verticalSpacing = Mathf.Clamp(verticalSpacing, 120f, 320f);
            blackboardWidth = Mathf.Clamp(blackboardWidth, 240f, 560f);
            inspectorWidth = Mathf.Clamp(inspectorWidth, 260f, 560f);
            panelHeight = Mathf.Clamp(panelHeight, 220f, 800f);
            runtimeRefreshMilliseconds = Mathf.Clamp(runtimeRefreshMilliseconds, 50, 1000);
            Save(true);
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/SWUtils/Behaviour Tree", SettingsScope.Project)
            {
                label = "SW Behaviour Tree",
                guiHandler = _ => DrawSettings(),
                keywords = new System.Collections.Generic.HashSet<string>
                {
                    "Behaviour", "Tree", "Node", "Blackboard", "Graph",
                },
            };
        }

        private static void DrawSettings()
        {
            SWBehaviourTreeEditorSettings settings = instance;
            EditorGUI.BeginChangeCheck();
            settings.nodeWidth = EditorGUILayout.Slider("Node Width", settings.nodeWidth, 180f, 360f);
            settings.horizontalSpacing = EditorGUILayout.Slider(
                "Horizontal Spacing", settings.horizontalSpacing, 10f, 120f);
            settings.verticalSpacing = EditorGUILayout.Slider(
                "Vertical Spacing", settings.verticalSpacing, 120f, 320f);
            settings.blackboardWidth = EditorGUILayout.Slider(
                "Blackboard Width", settings.blackboardWidth, 240f, 560f);
            settings.inspectorWidth = EditorGUILayout.Slider(
                "Inspector Width", settings.inspectorWidth, 260f, 560f);
            settings.panelHeight = EditorGUILayout.Slider(
                "Panel Height", settings.panelHeight, 220f, 800f);
            settings.showDescriptions = EditorGUILayout.Toggle(
                "Show Descriptions", settings.showDescriptions);
            settings.runtimeRefreshMilliseconds = EditorGUILayout.IntSlider(
                "Runtime Refresh", settings.runtimeRefreshMilliseconds, 50, 1000);
            if (EditorGUI.EndChangeCheck())
                settings.SaveSettings();
        }
    }
}
