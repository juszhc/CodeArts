﻿using System.Collections.Generic;

namespace CodeArts.ORM
{
    /// <summary>
    /// 查询能力
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        TResult QueryFirstOrDefault<TResult>(SQL sql, object param = null, int? commandTimeout = null);


        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <param name="missingMsg">未查询到数据时，异常信息。</param>
        /// <returns></returns>
        TResult QueryFirst<TResult>(SQL sql, object param = null, TResult defaultValue = default, int? commandTimeout = null, string missingMsg = null);

        /// <summary>
        /// 查询所有结果。
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        IEnumerable<TResult> Query<TResult>(SQL sql, object param = null, int? commandTimeout = null);
    }
}
