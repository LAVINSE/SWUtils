using System;

namespace SW.Attributes
{
    /// <summary>
    /// 에디터 + PlayMode에서 매 프레임 다시 그립니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SWRequiresConstantRepaintAttribute : Attribute
    {
        
    }
}
