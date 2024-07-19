using GraphQL.Types;
using LibplanetConsole.Explorer.Mutations;
using LibplanetConsole.Explorer.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace LibplanetConsole.Explorer.Schemas
{
    public class LibplanetExplorerSchema : Schema
    {
        public LibplanetExplorerSchema(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<ExplorerQuery>();
            Mutation = serviceProvider.GetRequiredService<ExplorerMutation>();
        }
    }
}
