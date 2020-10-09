﻿using System;
using System.Linq.Expressions;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// Contains。
    /// </summary>
    public class ContainsVisitor : BaseVisitor
    {
        /// <summary>
        /// inherit。
        /// </summary>
        public ContainsVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <summary>
        /// inherit。
        /// </summary>
        public override bool CanResolve(MethodCallExpression node) =>
            node.Method.Name == MethodCall.Contains && node.Arguments.Count == 2;

        /// <summary>
        /// inherit。
        /// </summary>
        protected override Expression StartupCore(MethodCallExpression node)
        {
            if (IsPlainVariable(node.Arguments[1]))
            {
                throw new NotSupportedException($"函数“Contains”的参数必须是主表达式的属性(如：.Contains(x.Id))!");
            }

            base.Visit(node.Arguments[1]);

            writer.Contains();

            writer.OpenBrace();

            using (var visitor = new SelectVisitor(this))
            {
                visitor.Startup(node.Arguments[0]);
            }

            writer.CloseBrace();

            return node;
        }
    }
}
