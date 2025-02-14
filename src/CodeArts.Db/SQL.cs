﻿using System.Collections.Generic;
#if NET40
using System.Collections.ObjectModel;
#endif
using System.Diagnostics;

namespace CodeArts.Db
{
    /// <summary>
    /// SQL 默认语法：
    ///     表名称：[yep_users]
    ///     名称：[name]
    ///     参数名称:{name}
    ///     条件移除：DROP TABLE IF EXIXSTS [yep_users];
    ///     条件创建：CREATE TABLE IF NOT EXIXSTS [yep_users] ([Id] int not null,[name] varchar(100));
    /// 说明：会自动去除代码注解和多余的换行符压缩语句。
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public sealed class SQL
    {
        private readonly string sql;
        private static readonly ISqlAdpter adpter;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static SQL() => adpter = RuntimeServPools.Singleton<ISqlAdpter, DefaultSqlAdpter>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">原始SQL语句。</param>
        public SQL(string sql) => this.sql = adpter.Analyze(sql);

        /// <summary>
        /// 添加语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        public SQL Add(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return this;
            }

            return new SQL(string.Concat(ToString(), ";", sql));
        }

        /// <summary>
        /// 添加语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        public SQL Add(SQL sql)
        {
            if (sql is null)
            {
                return this;
            }

            return Add(sql.ToString());
        }

#if NET40
        private ReadOnlyCollection<TableToken> tables;
        private ReadOnlyCollection<string> parameters;

        /// <summary>
        /// 操作的表。
        /// </summary>
        public ReadOnlyCollection<TableToken> Tables => tables ?? (tables = adpter.AnalyzeTables(sql));
        /// <summary>
        /// 参数。
        /// </summary>
        public ReadOnlyCollection<string> Parameters => parameters ?? (parameters = adpter.AnalyzeParameters(sql));
#else
        private IReadOnlyCollection<TableToken> tables;
        private IReadOnlyCollection<string> parameters;

        /// <summary>
        /// 操作的表。
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public IReadOnlyCollection<TableToken> Tables => tables ??= adpter.AnalyzeTables(sql);
#else
        public IReadOnlyCollection<TableToken> Tables => tables ?? (tables = adpter.AnalyzeTables(sql));
#endif

        /// <summary>
        /// 参数。
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public IReadOnlyCollection<string> Parameters => parameters ??= adpter.AnalyzeParameters(sql);
#else
        public IReadOnlyCollection<string> Parameters => parameters ?? (parameters = adpter.AnalyzeParameters(sql));
#endif
#endif

        /// <summary>
        /// 获取总行数。
        /// </summary>
        /// <returns></returns>
        public SQL ToCountSQL() => new SQL(adpter.ToCountSQL(sql));

        /// <summary>
        /// 获取分页数据。
        /// </summary>
        /// <param name="pageIndex">页码（从“0”开始）</param>
        /// <param name="pageSize">分页条数。</param>
        /// <returns></returns>
        public SQL ToSQL(int pageIndex, int pageSize) => new SQL(adpter.ToSQL(sql, pageIndex, pageSize));

        /// <summary>
        /// 转为实际数据库的SQL语句。
        /// </summary>
        /// <param name="settings">SQL修正配置。</param>
        /// <returns></returns>
        public string ToString(ISQLCorrectSettings settings) => adpter.Format(sql, settings);

        /// <summary>
        /// 返回分析的SQL结果。
        /// </summary>
        /// <returns></returns>
        public override string ToString() => adpter.Format(sql);

        /// <summary>
        /// 追加sql。
        /// </summary>
        /// <param name="left">原SQL。</param>
        /// <param name="right">需要追加的SQL。</param>
        /// <returns></returns>
        public static SQL operator +(SQL left, SQL right)
        {
            if (left is null)
            {
                return right;
            }

            if (right is null)
            {
                return left;
            }

            return left.Add(right);
        }

        /// <summary>
        /// 运算符。
        /// </summary>
        /// <param name="sql">SQL。</param>
        public static implicit operator SQL(string sql) => new SQL(sql);
    }
}
