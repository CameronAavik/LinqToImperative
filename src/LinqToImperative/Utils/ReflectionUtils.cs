using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToImperative.Utils
{
    internal static class ReflectionUtils
    {
        public static MethodInfo GetGenericMethod<T1>(Expression<Action<T1>> f) =>
            GetMethod(f).GetGenericMethodDefinition();

        public static MethodInfo GetGenericMethod<T1>(Expression<Func<T1, object>> f) => 
            GetMethod(f).GetGenericMethodDefinition();

        public static MethodInfo GetMethod<T1>(Expression<Action<T1>> f) => GetMethodRaw(f);

        public static MethodInfo GetMethod<T1>(Expression<Func<T1, object>> f) => GetMethodRaw(f);

        public static MethodInfo GetMethodRaw(LambdaExpression f) => f.Body switch
        {
            MethodCallExpression methodCall => methodCall.Method,
            UnaryExpression { Operand: MethodCallExpression methodCall } => methodCall.Method, // handle when casting occurs
            _ => throw new InvalidOperationException("Unable to extract method from expression")
        };
    }
}
