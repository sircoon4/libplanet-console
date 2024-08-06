using GraphQL.Types;
using Libplanet.Blockchain;
using Libplanet.Explorer.Indexing;
using Libplanet.Store;
using LibplanetConsole.Explorer.Interfaces;

namespace LibplanetConsole.Explorer.Mutations
{
    public class ExplorerMutation : ObjectGraphType
    {
        public ExplorerMutation(IBlockChainContext chainContext)
        {
            Name = "ExplorerMutation";

            Field<TransactionMutation>("transactionMutation", resolve: context => new { });
        }
    }
}
