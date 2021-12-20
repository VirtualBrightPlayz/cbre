using System;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Common {
    public static class ReflectionUtils {
        public static IEnumerable<Type> GetDerivedNonAbstract<T>() where T : class {
            var type = typeof(T);
            var assemblyTypes = type.Assembly.GetTypes().Where(t => !t.IsAbstract);
            return type switch {
                { IsInterface: true } => assemblyTypes.Where(t => t.GetInterfaces().Contains(type)),
                _ => assemblyTypes.Where(t => t.IsSubclassOf(type))
            };
        }
    }
}
