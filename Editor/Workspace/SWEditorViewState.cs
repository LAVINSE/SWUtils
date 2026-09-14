using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SW.EditorTools.Workspace
{
    /// <summary>
    /// 화면을 다시 생성해도 검색과 목록 위치를 유지합니다.
    /// </summary>
    [Serializable]
    public sealed class SWEditorViewState
    {
        /// <summary>상태를 공유할 화면 식별자입니다.</summary>
        public string Identifier;
        /// <summary>마지막 검색어입니다.</summary>
        public string SearchText = "";
        /// <summary>마지막 스크롤 위치입니다.</summary>
        public Vector2 ScrollPosition;
        /// <summary>사용자가 변경한 그룹 펼침 상태입니다.</summary>
        public List<SWEditorFoldoutState> Foldouts = new();

        /// <summary>
        /// 저장한 펼침 상태가 없을 때만 화면의 기본값을 사용합니다.
        /// </summary>
        public bool IsExpanded(string identifier, bool defaultValue)
        {
            SWEditorFoldoutState state = Foldouts.Find(item => item.Identifier == identifier);
            return state?.Expanded ?? defaultValue;
        }

        /// <summary>
        /// 검색으로 잠시 펼친 상태와 사용자가 직접 변경한 상태를 구분해 저장합니다.
        /// </summary>
        public void SetExpanded(string identifier, bool expanded)
        {
            SWEditorFoldoutState state = Foldouts.Find(item => item.Identifier == identifier);
            if (state == null)
            {
                state = new SWEditorFoldoutState { Identifier = identifier };
                Foldouts.Add(state);
            }

            state.Expanded = expanded;
        }
    }

    /// <summary>
    /// 한 목록 그룹의 펼침 상태입니다.
    /// </summary>
    [Serializable]
    public sealed class SWEditorFoldoutState
    {
        /// <summary>펼침 상태를 저장할 그룹 식별자입니다.</summary>
        public string Identifier;
        /// <summary>그룹의 펼침 여부입니다.</summary>
        public bool Expanded;
    }

    /// <summary>
    /// 목록 재생성 중 발생하는 임시 스크롤 초기화가 저장 상태를 덮어쓰지 않도록 합니다.
    /// </summary>
    internal sealed class SWEditorScrollKeeper
    {
        private readonly ScrollView scroll;
        private readonly Func<Vector2> readPosition;
        private readonly Action<Vector2> savePosition;
        private Vector2 restorePosition;
        private bool restoring;
        private int revision;

        /// <summary>
        /// 스크롤 변화와 목록 재생성을 화면의 저장 상태에 연결합니다.
        /// </summary>
        public SWEditorScrollKeeper(ScrollView scroll, Func<Vector2> readPosition, Action<Vector2> savePosition)
        {
            this.scroll = scroll;
            this.readPosition = readPosition;
            this.savePosition = savePosition;
            scroll.verticalScroller.valueChanged += SavePosition;
            scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(eventData =>
            {
                if (restoring)
                {
                    ScheduleRestore(revision);
                }
            });
        }

        /// <summary>
        /// 기존 위치를 보관한 뒤 새 목록의 배치가 끝나면 복원합니다.
        /// </summary>
        public void Rebuild(Action rebuild, bool resetPosition = false)
        {
            restorePosition = resetPosition ? Vector2.zero : readPosition();
            restoring = true;
            revision++;
            rebuild();
            ScheduleRestore(revision);
        }

        private void ScheduleRestore(int expectedRevision)
        {
            scroll.schedule.Execute(() =>
            {
                if (!restoring || revision != expectedRevision || scroll.panel == null)
                {
                    return;
                }

                if (scroll.contentViewport.layout.height <= 0 || float.IsNaN(scroll.contentViewport.layout.height))
                {
                    return;
                }

                scroll.scrollOffset = restorePosition;
                restoring = false;
                savePosition(scroll.scrollOffset);
            }).StartingIn(1);
        }

        private void SavePosition(float value)
        {
            if (!restoring && scroll.panel != null)
            {
                savePosition(new Vector2(0, value));
            }
        }
    }
}
