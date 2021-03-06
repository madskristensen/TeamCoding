﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamCoding
{
    /// <summary>
    /// Provides singleton instances of concrete types from the TeamCoding project implementing specified interfaces.
    /// If the instance was created using MEF or through something else then it won't re-use that.
    /// </summary>
    public static class TeamCodingProjectTypeProvider
    {
        private static readonly Dictionary<Type, object> CachedObjectInstances = new Dictionary<Type, object>();
        public static T Get<T>()
        {
            if (CachedObjectInstances.TryGetValue(typeof(T), out object o))
            {
                return (T)o;
            }
            
            o = Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblies()
                                                                .Single(a => a.GetName().Name == "TeamCoding")
                                                                .ExportedTypes.Single(t => typeof(T).IsAssignableFrom(t)));

            CachedObjectInstances.Add(typeof(T), o);

            return (T)o;
        }
    }
}
