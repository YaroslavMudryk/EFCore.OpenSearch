using OpenSearch.Client;
using System.Linq.Expressions;

namespace EFCore.OpenSearch;

public static class OpenSearchExpressionTranslator
{
    public static QueryContainer Translate(Expression expression, out int? skip, out int? take, out List<string> selectedFields, out SortDescriptor<dynamic> sortDescriptor, out bool trackTotalHits)
    {
        skip = null;
        take = null;
        selectedFields = [];
        sortDescriptor = new SortDescriptor<dynamic>();
        trackTotalHits = false;

        if (expression is MethodCallExpression methodCall)
        {
            return TranslateMethodCall(methodCall, ref skip, ref take, ref selectedFields, ref sortDescriptor, ref trackTotalHits);
        }

        throw new NotSupportedException($"Unsupported expression type: {expression.NodeType}");
    }

    private static QueryContainer TranslateMethodCall(MethodCallExpression methodCall, ref int? skip, ref int? take, ref List<string> selectedFields, ref SortDescriptor<dynamic> sortDescriptor, ref bool trackTotalHits)
    {
        if (methodCall.Method.Name == "Where")
        {
            return TranslateWhere(methodCall);
        }
        else if (methodCall.Method.Name == "OrderBy" || methodCall.Method.Name == "OrderByDescending")
        {
            sortDescriptor = TranslateOrderBy(methodCall);
            return TranslateMethodCall((MethodCallExpression)methodCall.Arguments[0], ref skip, ref take, ref selectedFields, ref sortDescriptor, ref trackTotalHits);
        }
        else if (methodCall.Method.Name == "Skip")
        {
            skip = (int)Expression.Lambda(((UnaryExpression)methodCall.Arguments[1]).Operand).Compile().DynamicInvoke();
            return TranslateMethodCall((MethodCallExpression)methodCall.Arguments[0], ref skip, ref take, ref selectedFields, ref sortDescriptor, ref trackTotalHits);
        }
        else if (methodCall.Method.Name == "Take")
        {
            take = (int)Expression.Lambda(((UnaryExpression)methodCall.Arguments[1]).Operand).Compile().DynamicInvoke();
            return TranslateMethodCall((MethodCallExpression)methodCall.Arguments[0], ref skip, ref take, ref selectedFields, ref sortDescriptor, ref trackTotalHits);
        }
        else if (methodCall.Method.Name == "Select")
        {
            selectedFields = TranslateSelect(methodCall);
            return TranslateMethodCall((MethodCallExpression)methodCall.Arguments[0], ref skip, ref take, ref selectedFields, ref sortDescriptor, ref trackTotalHits);
        }

        else if (methodCall.Method.Name == "Count" || methodCall.Method.Name == "LongCount")
        {
            trackTotalHits = true;
            return TranslateMethodCall((MethodCallExpression)methodCall.Arguments[0], ref skip, ref take, ref selectedFields, ref sortDescriptor, ref trackTotalHits);
        }

        throw new NotSupportedException($"Unsupported method: {methodCall.Method.Name}");
    }

    private static QueryContainer TranslateWhere(MethodCallExpression methodCall)
    {
        var lambda = (LambdaExpression)((UnaryExpression)methodCall.Arguments[1]).Operand;
        return ProcessExpression(lambda.Body);
    }

    private static QueryContainer ProcessExpression(Expression expression)
    {
        if (expression is BinaryExpression binaryExpression)
        {
            var field = ((MemberExpression)binaryExpression.Left).Member.Name;
            var value = Expression.Lambda(binaryExpression.Right).Compile().DynamicInvoke();

            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                    return new TermQuery { Field = field, Value = value };
                case ExpressionType.GreaterThan:
                    return new TermRangeQuery { Field = field, GreaterThan = value?.ToString() };
                case ExpressionType.GreaterThanOrEqual:
                    return new TermRangeQuery { Field = field, GreaterThanOrEqualTo = value?.ToString() };
                case ExpressionType.LessThan:
                    return new TermRangeQuery { Field = field, LessThan = value?.ToString() };
                case ExpressionType.LessThanOrEqual:
                    return new TermRangeQuery { Field = field, LessThanOrEqualTo = value?.ToString() };
                case ExpressionType.NotEqual:
                    return new BoolQuery { MustNot = new List<QueryContainer> { new TermQuery { Field = field, Value = value } } };
            }
        }
        else if (expression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Not)
        {
            var innerQuery = ProcessExpression(unaryExpression.Operand);
            return new BoolQuery { MustNot = new List<QueryContainer> { innerQuery } };
        }
        else if (expression is MethodCallExpression methodCall)
        {
            if (methodCall.Method.Name == "Contains")
            {
                var field = ((MemberExpression)methodCall.Object!).Member.Name;
                var value = Expression.Lambda(methodCall.Arguments[0]).Compile().DynamicInvoke();
                return new MatchQuery { Field = field, Query = value?.ToString() };
            }
        }
        else if (expression is BinaryExpression logicalExpression && (logicalExpression.NodeType == ExpressionType.AndAlso || logicalExpression.NodeType == ExpressionType.OrElse))
        {
            var leftQuery = ProcessExpression(logicalExpression.Left);
            var rightQuery = ProcessExpression(logicalExpression.Right);

            if (logicalExpression.NodeType == ExpressionType.AndAlso)
            {
                return new BoolQuery { Must = new List<QueryContainer> { leftQuery, rightQuery } };
            }
            else
            {
                return new BoolQuery { Should = new List<QueryContainer> { leftQuery, rightQuery }, MinimumShouldMatch = 1 };
            }
        }

        throw new NotSupportedException($"Unsupported expression: {expression.NodeType}");
    }

    private static SortDescriptor<dynamic> TranslateOrderBy(MethodCallExpression methodCall)
    {
        var lambda = (LambdaExpression)((UnaryExpression)methodCall.Arguments[1]).Operand;
        var field = ((MemberExpression)lambda.Body).Member.Name;

        var isDescending = methodCall.Method.Name == "OrderByDescending";

        return new SortDescriptor<dynamic>().Field(field, isDescending ? SortOrder.Descending : SortOrder.Ascending);
    }

    private static List<string> TranslateSelect(MethodCallExpression methodCall)
    {
        var lambda = (LambdaExpression)((UnaryExpression)methodCall.Arguments[1]).Operand;
        var memberInit = lambda.Body as NewExpression;

        if (memberInit == null)
        {
            throw new NotSupportedException("Only projections using new { x.Field1, x.Field2 } syntax are supported.");
        }

        return memberInit.Members.Select(m => m.Name).ToList();
    }
}
