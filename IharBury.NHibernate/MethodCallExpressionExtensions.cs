using System;
using System.Collections;
using System.Linq.Expressions;
using NHibernate.Linq;

namespace IharBury.NHibernate
{
    internal static class MethodCallExpressionExtensions
    {
        internal static bool IsCollectionFetch(this MethodCallExpression node)
        {
            if (node?.Method.DeclaringType == typeof(EagerFetchingExtensionMethods))
            {
                if (node.Method.Name == nameof(EagerFetchingExtensionMethods.FetchMany))
                    return true;

                if ((node.Method.Name == nameof(EagerFetchingExtensionMethods.Fetch)) &&
                        IsCollection(node.Method.GetGenericArguments()[1]))
                    return true;
            }

            return false;
        }

        internal static bool IsContinuationOfCollectionFetch(this MethodCallExpression node)
        {
            if (node?.Method.DeclaringType == typeof(EagerFetchingExtensionMethods))
            {
                if (node.Method.Name == nameof(EagerFetchingExtensionMethods.ThenFetchMany))
                    return true;

                if (node.Method.Name == nameof(EagerFetchingExtensionMethods.ThenFetch))
                    return true;
            }

            return false;
        }

        private static bool IsCollection(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type);
        }
    }
}
