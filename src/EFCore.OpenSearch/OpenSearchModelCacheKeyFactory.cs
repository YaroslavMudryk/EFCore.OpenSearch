using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.OpenSearch;

public class OpenSearchModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is OpenSearchDbContext openSearchContext)
        {
            var extension = openSearchContext.GetService<IDbContextOptions>().FindExtension<OpenSearchOptionsExtension>();
            return (context.GetType(), extension?.Client, extension?.IndexPrefix, designTime);
        }
        return new ModelCacheKey(context, designTime);
    }
}
