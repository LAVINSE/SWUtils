using System;

using SW.Debugging;

namespace SW.Attributes
{
    /// <summary>
    /// 메서드를 SWDebugConsole 명령으로 노출하는 어트리뷰트입니다.
    /// </summary>
    /// <remarks>
    /// 정적 메서드는 자동으로 수집되고, 인스턴스 메서드는
    /// <c>SWDebugConsole.RegisterInstance(this)</c>를 호출한 오브젝트에서만 동작합니다.
    /// 정수, 실수, Boolean, 문자열, 열거형 매개변수를 지원합니다.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class SWCommandAttribute : Attribute
    {
        #region 프로퍼티
        /// <summary>콘솔에서 입력할 명령 이름입니다. 비어 있으면 메서드 이름을 사용합니다.</summary>
        public string Name { get; }

        /// <summary>도움말과 치트 패널에 표시할 설명입니다.</summary>
        public string Description { get; }

        /// <summary>치트 패널에서 분류할 그룹 이름입니다.</summary>
        public string Group { get; }
        #endregion // 프로퍼티

        /// <summary>
        /// 명령 이름, 설명, 그룹을 지정해 콘솔 명령 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="name">명령 이름입니다. 비어 있으면 메서드 이름을 사용합니다.</param>
        /// <param name="description">명령 설명입니다.</param>
        /// <param name="group">치트 패널 그룹 이름입니다.</param>
        public SWCommandAttribute(string name = "", string description = "", string group = "")
        {
            Name = name;
            Description = description;
            Group = group;
        }
    }
}
