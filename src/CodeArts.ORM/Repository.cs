﻿using CodeArts.Config;
using CodeArts.ORM.Exceptions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据仓库
    /// </summary>
    public abstract class Repository
    {
        private static readonly ConcurrentDictionary<Type, DbConfigAttribute> mapperCache = new ConcurrentDictionary<Type, DbConfigAttribute>();

        private IDbConnectionAdapter _DbProvider = null;

        /// <summary>
        /// 链接配置
        /// </summary>
        protected IReadOnlyConnectionConfig ConnectionConfig { private set; get; }

        /// <summary>
        /// 数据库链接
        /// </summary>
        protected IDbConnection Connection => TransactionConnections.GetConnection(ConnectionConfig.ConnectionString, DbAdapter) ?? DispatchConnections.Instance.GetConnection(ConnectionConfig.ConnectionString, DbAdapter);

        /// <summary>
        /// 数据库适配器
        /// </summary>
        protected IDbConnectionAdapter DbAdapter => _DbProvider ?? (_DbProvider = DbConnectionManager.Get(ConnectionConfig.ProviderName));

        /// <summary>
        /// SQL矫正设置
        /// </summary>
        public ISQLCorrectSimSettings Settings => DbAdapter.Settings;

        /// <summary>
        /// 获取仓储数据库配置属性
        /// </summary>
        /// <returns></returns>
        private DbConfigAttribute GetAttribute() => mapperCache.GetOrAdd(GetType(), type =>
        {
            return (DbConfigAttribute)Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute), false) ?? (DbConfigAttribute)Attribute.GetCustomAttribute(type.BaseType, typeof(DbConfigAttribute), true) ?? throw new NotImplementedException("仓库类未指定数据库链接属性!");
        });

        /// <summary>
        /// 获取数据库配置
        /// </summary>
        /// <returns></returns>
        protected virtual ConnectionConfig GetDbConfig()
        {
            var attr = GetAttribute();

            ConfigHelper.Instance.OnConfigChanged += _ =>
            {
                ConnectionConfig = attr.GetConfig();

                if (_DbProvider is null || string.Equals(_DbProvider.ProviderName, ConnectionConfig.ProviderName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _DbProvider = null;
            };

            return attr.GetConfig();
        }

        /// <summary>
        /// 链接
        /// </summary>
        protected Repository() => ConnectionConfig = GetDbConfig() ?? throw new NoNullAllowedException("未找到数据链接配置信息!");
        /// <summary>
        /// 链接
        /// </summary>
        /// <param name="connectionConfig">链接配置</param>
        public Repository(IReadOnlyConnectionConfig connectionConfig) => ConnectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));

        /// <summary> 执行数据库事务 </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="factory">事件工厂</param>
        /// <param name="level">指定连接的事务锁定行为。</param>
        /// <returns></returns>
        protected TResult Transaction<TResult>(Func<IDbConnection, IDbTransaction, TResult> factory, IsolationLevel? level = null)
        {
            using (IDbConnection dbConnection = DispatchConnections.Instance.GetConnection(ConnectionConfig.ConnectionString, DbAdapter, false))
            {
                try
                {
                    dbConnection.Open();
                    using (var transaction = level.HasValue ? dbConnection.BeginTransaction(level.Value) : dbConnection.BeginTransaction())
                    {
                        try
                        {
                            var result = factory.Invoke(dbConnection, transaction);
                            transaction.Commit();
                            return result;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                finally
                {
                    dbConnection.Close();
                }
            }
        }
    }

    /// <summary>
    /// 数据仓储
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class Repository<T> : Repository, IRepository<T>, IOrderedQueryable<T>, IQueryable<T>, IEnumerable<T>, IOrderedQueryable, IQueryable, IQueryProvider, ISelectable, IEnumerable
    {
        private static ITableInfo tableInfo;
        /// <summary>
        /// 实体表信息
        /// </summary>
        public static ITableInfo TableInfo => tableInfo ?? (tableInfo = MapperRegions.Resolve(typeof(T)));

        /// <summary>
        /// 构造函数
        /// </summary>
        public Repository() : base()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionConfig">数据库链接</param>
        /// <param name="expression">表达式</param>
        private Repository(IReadOnlyConnectionConfig connectionConfig, Expression expression) : base(connectionConfig) => _Expression = expression ?? throw new ArgumentNullException(nameof(expression));

        /// <summary>
        /// 当前元素类型
        /// </summary>
        public Type ElementType => typeof(T);

        private readonly Expression _Expression = null;

        private Expression _ContextExpression = null;
        /// <summary>
        /// 查询供应器
        /// </summary>
        public IQueryProvider Provider => this;
        /// <summary>
        /// 迭代器
        /// </summary>
        protected IEnumerable<T> Enumerable { private set; get; }

        /// <summary>
        /// 仓库供应器
        /// </summary>
        protected virtual IDbRepositoryProvider DbProvider => DbConnectionManager.Create(DbAdapter);
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            Type type = expression.Type.FindGenericType(typeof(IRepository<>));

            if (type is null)
            {
                throw new ArgumentException("无效表达式!", nameof(expression));
            }

            Type type2 = typeof(Repository<>).MakeGenericType(type.GetGenericArguments());

            return (IQueryable)Activator.CreateInstance(type2, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[2]
            {
                ConnectionConfig,
                expression
            }, null);
        }
        /// <summary>
        /// 创建IQueryable
        /// </summary>
        /// <typeparam name="TElement">泛型类型</typeparam>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => new Repository<TElement>(ConnectionConfig, expression);

        /// <summary>
        /// 执行表达式
        /// </summary>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        object IQueryProvider.Execute(Expression expression) => Execute<T>(expression);

        /// <summary>
        /// 执行结果
        /// </summary>
        /// <typeparam name="TResult">结果</typeparam>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        private TResult Execute<TResult>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (!typeof(TResult).IsAssignableFrom(expression.Type))
            {
                throw new NotImplementedException(nameof(expression));
            }

            return DbProvider.Evaluate<T, TResult>(Connection, expression);
        }

        /// <summary>
        /// 执行表达式
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        TResult IQueryProvider.Execute<TResult>(Expression expression) => Execute<TResult>(expression);

        /// <summary>
        /// 表达式
        /// </summary>
        public Expression Expression => _Expression ?? _ContextExpression ?? (_ContextExpression = Expression.Constant(this));

        /// <summary>
        /// 查询SQL验证
        /// </summary>
        /// <returns></returns>
        protected virtual bool QueryAuthorize(ISQL sql) => sql.Tables.All(x => x.CommandType == CommandTypes.Select) && sql.Tables.Any(x => string.Equals(x.Name, TableInfo.TableName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// 构建参数。
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        protected virtual Dictionary<string, object> BuildParameters(ISQL sql, object param = null)
        {
            if (param is null)
            {
                if (sql.Parameters.Count > 0)
                {
                    throw new DSyntaxErrorException("参数少于SQL所需的参数!");
                }

                return null;
            }

            var type = param.GetType();

            if (type.IsValueType || type == typeof(string))
            {
                if (sql.Parameters.Count > 1)
                {
                    throw new DSyntaxErrorException("参数少于SQL所需的参数!");
                }

                var token = sql.Parameters.First();

                return new Dictionary<string, object>
                {
                    [token.Name] = param
                };
            }

            if (!(param is Dictionary<string, object> parameters))
            {
                parameters = param.MapTo<Dictionary<string, object>>();
            }

            if (parameters.Count < sql.Parameters.Count)
            {
                throw new DSyntaxErrorException("参数少于SQL所需的参数!");
            }

            if (!sql.Parameters.All(x => parameters.Any(y => y.Key == x.Name)))
            {
                throw new DSyntaxErrorException("参数不匹配!");
            }

            return parameters;
        }

        /// <summary>
        /// 查询一条数据(未查询到数据)
        /// </summary>
        /// <typeparam name="TResult">结果</typeparam>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        public virtual TResult QueryFirstOrDefault<TResult>(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (!QueryAuthorize(sql))
            {
                throw new NonAuthorizedException();
            }

            return DbProvider.QueryFirstOrDefault<TResult>(Connection, sql.ToString(Settings), BuildParameters(sql, param), commandTimeout);
        }

        /// <summary>
        /// 查询一条数据
        /// </summary>
        /// <typeparam name="TResult">结果</typeparam>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <param name="missingMsg">未查询到数据时，异常信息。</param>
        /// <returns></returns>
        public virtual TResult QueryFirst<TResult>(SQL sql, object param = null, TResult defaultValue = default, int? commandTimeout = null, string missingMsg = null)
        {
            if (!QueryAuthorize(sql))
            {
                throw new NonAuthorizedException();
            }

            return DbProvider.QueryFirst<TResult>(Connection, sql.ToString(Settings), BuildParameters(sql, param), default, commandTimeout, missingMsg);
        }

        /// <summary>
        /// 查询所有数据
        /// </summary>
        /// <typeparam name="TResult">结果</typeparam>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        public virtual IEnumerable<TResult> Query<TResult>(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (!QueryAuthorize(sql))
            {
                throw new NonAuthorizedException();
            }

            return DbProvider.Query<TResult>(Connection, sql.ToString(Settings), BuildParameters(sql, param), commandTimeout);

        }

        private IEnumerator<T> GetEnumerator()
        {
            if (_Expression == null)
            {
                return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
            }

            if (Enumerable == null)
            {
                Enumerable = Provider.Execute<IEnumerable<T>>(Expression);
            }

            return Enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    }
}
