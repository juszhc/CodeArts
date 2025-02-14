﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CodeArts
{
    /// <summary>
    /// 程序集缓存。
    /// </summary>
    public class AssemblyFinder
    {
        private static readonly string assemblyPath;

        private static readonly ConcurrentDictionary<string, Assembly> AassemblyLoads = new ConcurrentDictionary<string, Assembly>();
        private static readonly ConcurrentDictionary<string, IEnumerable<Assembly>> AssemblyCache = new ConcurrentDictionary<string, IEnumerable<Assembly>>();

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static AssemblyFinder()
        {
            if (!Directory.Exists(assemblyPath = AppDomain.CurrentDomain.RelativeSearchPath))
            {
                assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        /// <summary>
        /// 所有程序集。
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Assembly> FindAll() => Find("*.dll");

        /// <summary>
        /// 满足指定条件的程序集。
        /// </summary>
        /// <param name="pattern">DLL过滤规则。<see cref="Directory.GetFiles(string, string)"/></param>
        /// <returns></returns>
        public static IEnumerable<Assembly> Find(string pattern)
        {
            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (pattern.Length == 0)
            {
                throw new ArgumentException($"“{nameof(pattern)}”不能为空。", nameof(pattern));
            }

            if (!pattern.EndsWith(".dll"))
            {
                pattern += ".dll";
            }

            return AssemblyCache.GetOrAdd(pattern, searchPattern =>
            {
                Assembly[] assemblies = null;

                return Directory.GetFiles(assemblyPath, searchPattern).Select(x => AassemblyLoads.GetOrAdd(x, y =>
                {
                    if (assemblies is null)
                    {
                        assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    }

                    foreach (var assembly in assemblies)
                    {
                        if (assembly.IsDynamic)
                        {
                            continue;
                        }

                        if (string.Equals(y, assembly.Location, StringComparison.OrdinalIgnoreCase))
                        {
                            return assembly;
                        }
                    }

                    return Assembly.LoadFrom(y);
                })).ToList();
            });
        }
    }
}