using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SW.EditorTools.Util;

namespace SW.EditorTools
{
    /// <summary>SWUtils 편집기의 Slate Iris와 일반 인스펙터의 Iris Line이 공유하는 테마입니다.</summary>
    public static class SWEditorTheme
    {
        #region 색상
        /// <summary>편집기 창의 작업 영역과 내장 인스펙터 배경입니다.</summary>
        public static readonly Color Background = FromHex(0x1E2028);
        /// <summary>탐색 목록과 패널 배경입니다.</summary>
        public static readonly Color Panel = FromHex(0x282B36);
        /// <summary>분류 패널 배경입니다.</summary>
        public static readonly Color Sidebar = FromHex(0x242630);
        /// <summary>도구 모음과 버튼 배경입니다.</summary>
        public static readonly Color Toolbar = FromHex(0x252832);
        /// <summary>카드 배경입니다.</summary>
        public static readonly Color Card = FromHex(0x2D303C);
        /// <summary>
        /// 입력 가능한 영역을 패널과 구분하는 배경입니다.
        /// </summary>
        public static readonly Color Input = FromHex(0x323645);
        /// <summary>얇은 패널 테두리입니다.</summary>
        public static readonly Color Border = FromHex(0x474B5E);
        /// <summary>주요 글자 색상입니다.</summary>
        public static readonly Color Text = FromHex(0xEAEAF2);
        /// <summary>보조 글자 색상입니다.</summary>
        public static readonly Color MutedText = FromHex(0xB5B8CB);
        /// <summary>선택한 항목 배경입니다.</summary>
        public static readonly Color Selection = FromHex(0x3E3B58);
        /// <summary>포인터가 올라간 항목의 배경입니다.</summary>
        public static readonly Color Hover = FromHex(0x363A4B);
        /// <summary>강조 색상입니다.</summary>
        public static readonly Color Accent = FromHex(0xB3ACEE);
        #endregion // 색상

        #region 공통 크기
        /// <summary>본문의 기본 글자 크기입니다. 공통 스타일 시트의 본문 크기와 함께 유지합니다.</summary>
        public const int BodyFontSize = 14;
        /// <summary>경로와 상태 표식에 사용하는 보조 글자 크기입니다.</summary>
        public const int SecondaryFontSize = 12;
        /// <summary>자동 배치되는 버튼과 입력 영역의 기본 높이입니다.</summary>
        public const float ControlHeight = 32f;
        /// <summary>그래프 도구 모음에서 버튼과 위아래 여백을 확보하는 높이입니다.</summary>
        public const float GraphToolbarHeight = 42f;
        #endregion // 공통 크기

        #region 필드
        private static StyleSheet sharedStyleSheet;
        #endregion // 필드

        #region 테마 적용
        /// <summary>루트에 공통 스타일을 적용합니다. 루트가 없으면 적용하지 않습니다.</summary>
        public static void Apply(VisualElement root)
        {
            if (root == null)
            {
                return;
            }
            sharedStyleSheet ??= SWEditorUtils.LoadStyleSheetByIdentifier("ad56e70652fd2fd45aa2c17f8094bfc1");
            root.AddToClassList("sw-theme");
            if (sharedStyleSheet != null && !root.styleSheets.Contains(sharedStyleSheet))
            {
                root.styleSheets.Add(sharedStyleSheet);
            }
        }

        /// <summary>
        /// 일반 인스펙터에 공통 컨트롤과 Iris Line 전용 색상 및 크기를 적용합니다.
        /// 루트가 없으면 적용하지 않습니다.
        /// </summary>
        public static void ApplyInspector(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            Apply(root);
            root.AddToClassList("sw-inspector-theme");
        }

        /// <summary>
        /// 작업 공간에 삽입한 인스펙터의 기본 그룹 색과 여백을 공통 테마에 맞춥니다.
        /// 사용자가 지정한 그룹 색은 유지합니다.
        /// </summary>
        public static void ApplyEmbeddedInspector(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            Apply(root);
            root.RemoveFromClassList("sw-inspector-theme");
            root.Query<VisualElement>(className: "sw-inspector-theme").ForEach(inspector =>
            {
                inspector.RemoveFromClassList("sw-inspector-theme");
            });
            root.AddToClassList("sw-embedded-inspector");
        }
        #endregion // 테마 적용

        #region 색상 변환
        /// <summary>정수 색상값을 Unity 색상으로 변환합니다.</summary>
        private static Color FromHex(int value)
        {
            return new Color32((byte)(value >> 16), (byte)(value >> 8), (byte)value, 255);
        }
        #endregion // 색상 변환
    }

    /// <summary>편집기 창과 내장 인스펙터를 그리는 동안 공통 스타일을 적용하고 원래 스타일을 복원합니다.</summary>
    public sealed class SWEditorThemeScope : IDisposable
    {
        private sealed class StylePair
        {
            public GUIStyle Target;
            public GUIStyle Original;
            public GUIStyle Themed;
        }

        private static readonly List<StylePair> stylePairs = new();
        private static readonly List<Texture2D> textures = new();
        private static GUISkin themedSkin;
        private static int nesting;
        /// <summary>현재 그리는 코드가 편집기 창의 테마 적용 범위 안에 있는지 나타냅니다.</summary>
        internal static bool IsActive => nesting > 0;

        private readonly GUISkin originalSkin;
        private bool disposed;
        /// <summary>현재 그리는 범위에만 공통 테마를 적용합니다.</summary>
        public SWEditorThemeScope(Rect? background = null)
        {
            originalSkin = GUI.skin;
            if (nesting++ == 0)
            {
                EnsureStyles();
                GUI.skin = themedSkin;
                foreach (StylePair pair in stylePairs)
                    CopyAppearance(pair.Target, pair.Themed);
            }

            if (background.HasValue && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(background.Value, SWEditorTheme.Background);
        }

        /// <summary>다른 Unity 창에 테마가 남지 않도록 원래 스타일을 복원합니다.</summary>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (--nesting == 0)
            {
                foreach (StylePair pair in stylePairs)
                    CopyAppearance(pair.Target, pair.Original);
                GUI.skin = originalSkin;
            }
        }

        private static void EnsureStyles()
        {
            if (themedSkin != null)
                return;
            themedSkin = UnityEngine.Object.Instantiate(GUI.skin);
            themedSkin.hideFlags = HideFlags.HideAndDontSave;
            Texture2D normal = CreateBackground(SWEditorTheme.Toolbar, SWEditorTheme.Border);
            Texture2D hover = CreateBackground(SWEditorTheme.Card, SWEditorTheme.Accent);
            Texture2D selected = CreateBackground(SWEditorTheme.Selection, SWEditorTheme.Accent);
            Texture2D input = CreateBackground(SWEditorTheme.Input, SWEditorTheme.Border);
            Texture2D popup = CreateBackground(SWEditorTheme.Input, SWEditorTheme.Border, true);
            Texture2D popupHover = CreateBackground(SWEditorTheme.Input, SWEditorTheme.Accent, true);
            Texture2D panel = CreateBackground(SWEditorTheme.Panel, SWEditorTheme.Border);
            foreach (GUIStyle style in themedSkin.customStyles)
                ThemeStyle(style, null, null, null);
            ThemeStyle(themedSkin.label, null, null, null);
            ThemeStyle(themedSkin.button, normal, hover, selected);
            ThemeStyle(themedSkin.box, panel, panel, panel);
            ThemeStyle(themedSkin.textField, input, input, input);
            ThemeStyle(themedSkin.textArea, input, input, input);
            ThemeStyle(themedSkin.toggle, null, null, null);
            AddStyle(EditorStyles.label, null, null, null);
            AddStyle(EditorStyles.boldLabel, null, null, null);
            AddStyle(EditorStyles.miniLabel, null, null, null);
            AddStyle(EditorStyles.miniBoldLabel, null, null, null);
            AddStyle(EditorStyles.largeLabel, null, null, null);
            AddStyle(EditorStyles.wordWrappedLabel, null, null, null);
            AddStyle(EditorStyles.wordWrappedMiniLabel, null, null, null);
            AddStyle(EditorStyles.textField, input, input, input);
            AddStyle(EditorStyles.numberField, input, input, input);
            AddStyle(EditorStyles.popup, popup, popupHover, popupHover);
            AddStyle(EditorStyles.objectField, input, input, input);
            AddStyle(EditorStyles.toggle, null, null, null);
            AddStyle(EditorStyles.foldout, null, null, null);
            AddStyle(EditorStyles.foldoutHeader, null, null, null);
            AddStyle(EditorStyles.helpBox, panel, panel, panel);
            AddStyle(EditorStyles.toolbar, normal, normal, normal);
            AddStyle(EditorStyles.toolbarButton, normal, hover, selected);
            AddStyle(EditorStyles.toolbarPopup, popup, popupHover, popupHover);
            AddStyle(EditorStyles.toolbarTextField, input, input, input);
            AddStyle(EditorStyles.toolbarSearchField, input, input, input);
            AddStyle(EditorStyles.miniButton, normal, hover, selected);
            AddStyle(EditorStyles.miniButtonLeft, normal, hover, selected);
            AddStyle(EditorStyles.miniButtonMid, normal, hover, selected);
            AddStyle(EditorStyles.miniButtonRight, normal, hover, selected);
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.quitting += Release;
        }

        private static void AddStyle(GUIStyle target, Texture2D normal, Texture2D hover, Texture2D selected)
        {
            if (stylePairs.Exists(pair => ReferenceEquals(pair.Target, target)))
                return;
            GUIStyle themed = new(target);
            ThemeStyle(themed, normal, hover, selected);
            if (normal != null && normal.width > 8)
            {
                themed.fixedHeight = 0f;
            }
            stylePairs.Add(new StylePair { Target = target, Original = new GUIStyle(target), Themed = themed });
        }

        /// <summary>지정한 스타일의 글자 가독성과 상태별 테마 색상을 적용합니다.</summary>
        private static void ThemeStyle(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D selected)
        {
            if (style.fontSize <= 0)
            {
                style.fontSize = SWEditorTheme.BodyFontSize;
            }
            else if (style.fontSize < SWEditorTheme.SecondaryFontSize)
            {
                style.fontSize = SWEditorTheme.SecondaryFontSize;
            }

            GUIStyleState[] states =
            {
                style.normal,
                style.hover,
                style.active,
                style.focused,
                style.onNormal,
                style.onHover,
                style.onActive,
                style.onFocused
            };
            for (int index = 0; index < states.Length; index++)
            {
                states[index].textColor = SWEditorTheme.Text;
                if (normal == null)
                {
                    continue;
                }
                states[index].background = index >= 4 ? selected : index == 1 ? hover : normal;
                states[index].scaledBackgrounds = Array.Empty<Texture2D>();
            }

            if (normal != null)
            {
                style.border = normal.width > 8 ? new RectOffset(4, 16, 16, 16) : new RectOffset(4, 4, 4, 4);
            }
        }

        private static void CopyAppearance(GUIStyle target, GUIStyle source)
        {
            target.normal = source.normal;
            target.hover = source.hover;
            target.active = source.active;
            target.focused = source.focused;
            target.onNormal = source.onNormal;
            target.onHover = source.onHover;
            target.onActive = source.onActive;
            target.onFocused = source.onFocused;
            target.border = source.border;
            target.fontSize = source.fontSize;
            target.fixedHeight = source.fixedHeight;
        }

        /// <summary>입력 영역과 선택 메뉴에 사용하는 테두리 및 화살표 배경을 만듭니다.</summary>
        private static Texture2D CreateBackground(Color fill, Color border, bool dropdown = false)
        {
            int width = dropdown ? 24 : 8;
            int height = dropdown ? 32 : 8;
            Texture2D texture = new(width, height)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            Color[] pixels = new Color[width * height];
            for (int vertical = 0; vertical < height; vertical++)
            {
                for (int horizontal = 0; horizontal < width; horizontal++)
                {
                    bool edge = horizontal == 0 || vertical == 0 || horizontal == width - 1 || vertical == height - 1;
                    bool corner = (horizontal == 0 || horizontal == width - 1) && (vertical == 0 || vertical == height - 1);
                    bool arrow = dropdown && vertical >= 10 && vertical <= 16 && Mathf.Abs(horizontal - (width - 9)) == (vertical - 10) / 2;
                    pixels[vertical * width + horizontal] = arrow ? SWEditorTheme.MutedText : corner ? Color.clear : edge ? border : fill;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            textures.Add(texture);
            return texture;
        }

        private static void Release()
        {
            if (nesting > 0)
                foreach (StylePair pair in stylePairs)
                    CopyAppearance(pair.Target, pair.Original);
            foreach (Texture2D texture in textures)
                UnityEngine.Object.DestroyImmediate(texture);
            if (themedSkin != null)
                UnityEngine.Object.DestroyImmediate(themedSkin);
            textures.Clear();
            stylePairs.Clear();
            nesting = 0;
        }
    }
}
