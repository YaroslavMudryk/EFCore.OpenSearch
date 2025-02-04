using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace EFCore.OpenSearch;

public class OpenSearchQueryCompiler : QueryCompiler
{
    private readonly IQueryContextFactory _queryContextFactory;

    public OpenSearchQueryCompiler(
        IQueryContextFactory queryContextFactory,
        ICompiledQueryCache compiledQueryCache,
        ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator,
        IDatabase database,
        IDiagnosticsLogger<DbLoggerCategory.Query> diagnosticsLogger,
        ICurrentDbContext currentDbContext,
        IEvaluatableExpressionFilter evaluatableExpressionFilter,
        IModel model,
        QueryCompilationContextFactory queryCompilationContextFactory)
        : base(queryContextFactory, compiledQueryCache, compiledQueryCacheKeyGenerator, database, diagnosticsLogger, currentDbContext, evaluatableExpressionFilter, model)
    {
        _queryContextFactory = queryContextFactory;
    }

    public IQueryable<T> CompileQuery<T>(Expression expression)
    {
        return new OpenSearchQueryable<T>(
            new OpenSearchQueryProvider<T>(
                ((OpenSearchQueryContext)_queryContextFactory.Create()).Client,
                ((OpenSearchQueryContext)_queryContextFactory.Create()).IndexName),
            expression);
    }
}
