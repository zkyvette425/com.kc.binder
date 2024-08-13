using System;
using UnityEngine;

namespace KC
{
    internal class BinderMemberInfo
    {
        /// <summary>
        /// 成员名称
        /// </summary>
        public string MemberName { get; set; }
            
        /// <summary>
        /// 注释信息
        /// </summary>
        public string Annotation { get; set; }
            
        /// <summary>
        /// 类型
        /// </summary>
        public Type Type { get; set; }
            
        /// <summary>
        /// 实例
        /// </summary>
        public Transform Transform { get; set; }
            
        /// <summary>
        /// 成员信息集
        /// </summary>
        public BinderMemberInfo[] MemberInfos { get; set; }

        /// <summary>
        /// 是否是有值数组
        /// </summary>
        public bool IsArray => MemberInfos is { Length: > 0 };
    }
}