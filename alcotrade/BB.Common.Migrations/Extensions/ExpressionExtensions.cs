using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BB.Common.Migrations.Extensions
{
    public static class ExpressionExtensions
    {
        public static IEnumerable<PropertyPath> GetPropertyAccessList(this LambdaExpression propertyAccessExpression)
        {
            var propertyPaths
                = MatchPropertyAccessList(propertyAccessExpression, (p, e) => MatchPropertyAccess(e, p));

            if (propertyPaths == null)
            {
                throw new Exception(propertyAccessExpression.Body.ToString());
            }

            return propertyPaths;
        }

        public static PropertyPath GetSimplePropertyAccess(this LambdaExpression propertyAccessExpression)
        {
            var propertyPath = MatchSimplePropertyAccess(propertyAccessExpression.Parameters.Single(), propertyAccessExpression.Body);
            if (propertyPath == null)
                throw new Exception(propertyAccessExpression.Body.ToString());
            return propertyPath;
        }

        public static PropertyPath GetComplexPropertyAccess(this LambdaExpression propertyAccessExpression)
        {
            var propertyPath = MatchComplexPropertyAccess(propertyAccessExpression.Parameters.Single(), propertyAccessExpression.Body);
            if (propertyPath == null)
                throw new Exception(propertyAccessExpression.Body.ToString());
            return propertyPath;
        }

        public static IEnumerable<PropertyPath> GetSimplePropertyAccessList(this LambdaExpression propertyAccessExpression)
        {
            var enumerable = MatchPropertyAccessList(propertyAccessExpression, (p, e) => MatchSimplePropertyAccess(e, p));
            if (enumerable == null)
                throw new Exception(propertyAccessExpression.Body.ToString());
            return enumerable;
        }

        public static IEnumerable<PropertyPath> GetComplexPropertyAccessList(this LambdaExpression propertyAccessExpression)
        {
            var enumerable = MatchPropertyAccessList(propertyAccessExpression, (p, e) => MatchComplexPropertyAccess(e, p));
            if (enumerable == null)
                throw new Exception(propertyAccessExpression.Body.ToString());
            return enumerable;
        }

        private static IEnumerable<PropertyPath> MatchPropertyAccessList(this LambdaExpression lambdaExpression, Func<Expression, Expression, PropertyPath> propertyMatcher)
        {
            var newExpression = RemoveConvert(lambdaExpression.Body) as NewExpression;
            if (newExpression != null)
            {
                var parameterExpression = lambdaExpression.Parameters.Single();
                var enumerable = newExpression.Arguments.Select(a => propertyMatcher(a, parameterExpression)).Where(p => p != (PropertyPath)null);
                if (enumerable.Count() == newExpression.Arguments.Count())
                {
                    return !HasDefaultMembersOnly(newExpression, enumerable) ? null : enumerable;
                }
            }
            var propertyPath = propertyMatcher(lambdaExpression.Body, lambdaExpression.Parameters.Single());
            if (!(propertyPath != null))
                return null;
            return new PropertyPath[1]
            {
                propertyPath
            };
        }

        private static bool HasDefaultMembersOnly(this NewExpression newExpression, IEnumerable<PropertyPath> propertyPaths)
        {
            return !newExpression.Members.Where((t, i) => !string.Equals(t.Name, propertyPaths.ElementAt(i).Last().Name, StringComparison.Ordinal)).Any();
        }

        private static PropertyPath MatchSimplePropertyAccess(this Expression parameterExpression, Expression propertyAccessExpression)
        {
            var propertyPath = MatchPropertyAccess(parameterExpression, propertyAccessExpression);
            if (!(propertyPath != null) || propertyPath.Count != 1)
                return null;
            return propertyPath;
        }

        private static PropertyPath MatchComplexPropertyAccess(this Expression parameterExpression, Expression propertyAccessExpression)
        {
            return MatchPropertyAccess(parameterExpression, propertyAccessExpression);
        }

        private static PropertyPath MatchPropertyAccess(this Expression parameterExpression, Expression propertyAccessExpression)
        {
            var list = new List<PropertyInfo>();
            MemberExpression memberExpression;
            do
            {
                memberExpression = RemoveConvert(propertyAccessExpression) as MemberExpression;
                if (memberExpression == null)
                    return null;
                var propertyInfo = memberExpression.Member as PropertyInfo;
                if (propertyInfo == null)
                    return null;
                list.Insert(0, propertyInfo);
                propertyAccessExpression = memberExpression.Expression;
            }
            while (memberExpression.Expression != parameterExpression);
            return new PropertyPath(list);
        }

        public static Expression RemoveConvert(this Expression expression)
        {
            while (expression != null && (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked))
                expression = RemoveConvert(((UnaryExpression)expression).Operand);
            return expression;
        }
    }
}