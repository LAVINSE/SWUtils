using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SW.EditorTools.Util
{
    /// <summary>설치된 입력 방식에 맞춰 디버거의 입력 상태를 읽습니다. 장치가 없으면 기본값을 반환합니다.</summary>
    internal static class SWEditorInputUtility
    {
        #region 필드
        private static readonly Dictionary<(Type, string), PropertyInfo> properties = new();
        private static readonly Dictionary<Type, MethodInfo> readMethods = new();
        private static readonly string[] mouseButtons = { "leftButton", "rightButton", "middleButton" };
        private static readonly Type mouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
        private static readonly Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
        private static readonly Type touchscreenType = Type.GetType("UnityEngine.InputSystem.Touchscreen, Unity.InputSystem");
        #endregion // 필드

        #region 마우스
        /// <summary>마우스 화면 좌표를 읽습니다. 마우스가 없으면 영벡터를 반환합니다.</summary>
        public static Vector2 ReadMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return ReadControl(GetProperty(GetCurrentDevice(mouseType), "position")) is Vector2 value ? value : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        /// <summary>현재 입력 방식의 마우스 스크롤 값을 읽습니다. 장치가 없으면 영벡터를 반환합니다.</summary>
        public static Vector2 ReadMouseScroll()
        {
#if ENABLE_INPUT_SYSTEM
            return ReadControl(GetProperty(GetCurrentDevice(mouseType), "scroll")) is Vector2 value ? value : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta;
#else
            return Vector2.zero;
#endif
        }

        /// <summary>왼쪽, 오른쪽, 가운데 버튼 상태를 읽습니다. 지원하지 않는 번호는 false를 반환합니다.</summary>
        public static bool IsMouseButtonPressed(int button, bool pressedThisFrame = false)
        {
            if (button < 0 || button >= mouseButtons.Length)
            {
                return false;
            }
#if ENABLE_INPUT_SYSTEM
            object control = GetProperty(GetCurrentDevice(mouseType), mouseButtons[button]);
            return GetProperty(control, pressedThisFrame ? "wasPressedThisFrame" : "isPressed") is true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return pressedThisFrame ? Input.GetMouseButtonDown(button) : Input.GetMouseButton(button);
#else
            return false;
#endif
        }
        #endregion // 마우스

        #region 키보드와 터치
        /// <summary>전체 키 또는 보조 키 상태를 읽습니다. 지원하지 않는 이름이나 없는 장치는 false를 반환합니다.</summary>
        public static bool IsKeyboardControlPressed(string controlName)
        {
#if ENABLE_INPUT_SYSTEM
            return GetProperty(GetProperty(GetCurrentDevice(keyboardType), controlName), "isPressed") is true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return controlName switch
            {
                "anyKey" => Input.anyKey,
                "shiftKey" => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                "ctrlKey" => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                "altKey" => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
                _ => false
            };
#else
            return false;
#endif
        }

        /// <summary>활성 터치 수와 최대 다섯 개의 표시 문구를 읽습니다. 터치 장치가 없으면 0을 반환합니다.</summary>
        public static int ReadTouchDescriptions(List<string> descriptions)
        {
            descriptions.Clear();
            int count = 0;
#if ENABLE_INPUT_SYSTEM
            if (GetProperty(GetCurrentDevice(touchscreenType), "touches") is not IEnumerable touches)
            {
                return 0;
            }

            foreach (object touch in touches)
            {
                if (GetProperty(GetProperty(touch, "press"), "isPressed") is not true)
                {
                    continue;
                }
                count++;
                if (descriptions.Count < 5)
                {
                    object phase = ReadControl(GetProperty(touch, "phase"));
                    object identifier = ReadControl(GetProperty(touch, "touchId"));
                    Vector2 position = ReadControl(GetProperty(touch, "position")) is Vector2 value ? value : Vector2.zero;
                    descriptions.Add($"{phase} @ {position:F0} (식별자={identifier})");
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            count = Input.touchCount;
            for (int index = 0; index < Mathf.Min(count, 5); index++)
            {
                Touch touch = Input.GetTouch(index);
                descriptions.Add($"{touch.phase} @ {touch.position:F0} (식별자={touch.fingerId})");
            }
#endif
            return count;
        }
        #endregion // 키보드와 터치

        #region 선택적 패키지 조회
        /// <summary>선택적 입력 패키지의 현재 장치를 조회합니다. 패키지나 장치가 없으면 null을 반환합니다.</summary>
        private static object GetCurrentDevice(Type type)
        {
            if (type == null)
            {
                return null;
            }
            return GetPropertyInfo(type, "current")?.GetValue(null);
        }

        /// <summary>캐시한 공개 속성을 읽습니다. 대상이나 속성이 없으면 null을 반환합니다.</summary>
        private static object GetProperty(object target, string name)
        {
            return target == null ? null : GetPropertyInfo(target.GetType(), name)?.GetValue(target);
        }

        /// <summary>공개 속성 정보를 캐시합니다. 일치하는 속성이 없으면 null을 반환합니다.</summary>
        private static PropertyInfo GetPropertyInfo(Type type, string name)
        {
            var key = (type, name);
            if (!properties.TryGetValue(key, out PropertyInfo property))
            {
                property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                properties[key] = property;
            }
            return property;
        }

        /// <summary>입력 컨트롤 값을 읽습니다. 컨트롤이나 읽기 함수가 없으면 null을 반환합니다.</summary>
        private static object ReadControl(object control)
        {
            if (control == null)
            {
                return null;
            }

            Type type = control.GetType();
            if (!readMethods.TryGetValue(type, out MethodInfo method))
            {
                method = type.GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                readMethods[type] = method;
            }
            return method?.Invoke(control, null);
        }
        #endregion // 선택적 패키지 조회
    }
}
