using System.Collections.Generic;

namespace KC
{
    /// <summary>
    /// 类信息
    /// </summary>
    internal class BinderClassInfo
    {
        /// <summary>
        /// 类名称
        /// </summary>
        public string ClassName { get; set; }
            
        /// <summary>
        /// 类注释
        /// </summary>
        public string Annotation { get; set; }
            
        /// <summary>
        /// 存放路径
        /// </summary>
        public string Path { get; set; }
            
        /// <summary>
        /// 成员信息集
        /// </summary>
        public List<BinderMemberInfo> MemberInfos { get; set; }
    }
}