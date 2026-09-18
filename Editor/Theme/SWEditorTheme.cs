using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SW.EditorTools.Util;

namespace SW.EditorTools
{
    /// <summary>SWUtils 편집기 창과 창 내부의 편집 영역에서 공유하는 디자인입니다.</summary>
    public static class SWEditorTheme
    {
        /// <summary>편집기 창의 작업 영역과 내장 인스펙터 배경입니다.</summary>
        public static readonly Color Background = FromHex(0x13161A);
        /// <summary>탐색 목록과 패널 배경입니다.</summary>
        public static readonly Color Panel = FromHex(0x1B1F24);
        /// <summary>분류 패널 배경입니다.</summary>
        public static readonly Color Sidebar = FromHex(0x222831);
        /// <summary>도구 모음과 버튼 배경입니다.</summary>
        public static readonly Color Toolbar = FromHex(0x1F242B);
        /// <summary>카드 배경입니다.</summary>
        public static readonly Color Card = FromHex(0x272D35);
        /// <summary>
        /// 입력 가능한 영역을 패널과 구분하는 배경입니다.
        /// </summary>
        public static readonly Color Input = FromHex(0x242C35);
        /// <summary>얇은 패널 테두리입니다.</summary>
        public static readonly Color Border = FromHex(0x424E5B);
        /// <summary>주요 글자 색상입니다.</summary>
        public static readonly Color Text = FromHex(0xE7EEF5);
        /// <summary>보조 글자 색상입니다.</summary>
        public static readonly Color MutedText = FromHex(0xA8B9C8);
        /// <summary>선택한 항목 배경입니다.</summary>
        public static readonly Color Selection = FromHex(0x2A5B89);
        /// <summary>강조 색상입니다.</summary>
        public static readonly Color Accent = FromHex(0x80BFFF);
        private static StyleSheet sharedStyleSheet;
        /// <summary>편집기 창의 루트에 공통 스타일을 적용합니다. 일반 인스펙터에서는 호출하지 않습니다.</summary>
        public static void Apply(VisualElement root)
        {
            if (root == null)
                return;
            sharedStyleSheet ??= SWEditorUtils.LoadStyleSheetByIdentifier("ad56e70652fd2fd45aa2c17f8094bfc1");
            root.AddToClassList("sw-theme");
            if (sharedStyleSheet != null && !root.styleSheets.Contains(sharedStyleSheet))
                root.styleSheets.Add(sharedStyleSheet);
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
            root.AddToClassList("sw-embedded-inspector");

            Color defaultGroupColor = new SW.Attributes.SWGroupAttribute(string.Empty).GroupColor;
            root.Query<Foldout>(className: "sw-foldout").ForEach(group =>
            {
                if (group.style.borderLeftColor.value == defaultGroupColor)
                {
                    group.style.borderLeftColor = Accent;
                }
            });
        }

        /// <summary>정수 색상값을 Unity 색상으로 변환합니다.</summary>
        private static Color FromHex(int value)
        {
            return new Color32((byte)(value >> 16), (byte)(value >> 8), (byte)value, 255);
        }
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
            AddStyle(EditorStyles.largeLabel, null, null, null);
            AddStyle(EditorStyles.wordWrappedLabel, null, null, null);
            AddStyle(EditorStyles.wordWrappedMiniLabel, null, null, null);
            AddStyle(EditorStyles.textField, input, input, input);
            AddStyle(EditorStyles.numberField, input, input, input);
            AddStyle(EditorStyles.popup, null, null, null);
            AddStyle(EditorStyles.objectField, null, null, null);
            AddStyle(EditorStyles.toggle, null, null, null);
            AddStyle(EditorStyles.foldout, null, null, null);
            AddStyle(EditorStyles.foldoutHeader, null, null, null);
            AddStyle(EditorStyles.helpBox, panel, panel, panel);
            AddStyle(EditorStyles.toolbar, normal, normal, normal);
            AddStyle(EditorStyles.toolbarButton, normal, hover, selected);
            AddStyle(EditorStyles.toolbarPopup, null, null, null);
            AddStyle(EditorStyles.toolbarTextField, input, input, input);
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
            stylePairs.Add(new StylePair { Target = target, Original = new GUIStyle(target), Themed = themed });
        }

        private static void ThemeStyle(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D selected)
        {
            if (style.fontSize <= 0)
            {
                style.fontSize = 13;
            }
            else if (style.fontSize < 12)
            {
                style.fontSize = 12;
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
                    continue;
                states[index].background = index >= 4 ? selected : index == 1 ? hover : normal;
                states[index].scaledBackgrounds = Array.Empty<Texture2D>();
            }

            if (normal != null)
                style.border = new RectOffset(4, 4, 4, 4);
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
        }

        private static Texture2D CreateBackground(Color fill, Color border)
        {
            Texture2D texture = new(8, 8)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            Color[] pixels = new Color[64];
            for (int vertical = 0; vertical < 8; vertical++)
                for (int horizontal = 0; horizontal < 8; horizontal++)
                {
                    bool edge = horizontal == 0 || vertical == 0 || horizontal == 7 || vertical == 7;
                    bool corner = (horizontal == 0 || horizontal == 7) && (vertical == 0 || vertical == 7);
                    pixels[vertical * 8 + horizontal] = corner ? Color.clear : edge ? border : fill;
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
