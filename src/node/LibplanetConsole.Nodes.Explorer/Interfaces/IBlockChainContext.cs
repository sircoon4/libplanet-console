using System.Runtime.CompilerServices;
using GraphQL.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Explorer.Indexing;
using Libplanet.Explorer.Queries;
using Libplanet.Net;
using Libplanet.Store;
using LibplanetConsole.Nodes;

namespace LibplanetConsole.Explorer.Interfaces
{
    public interface IBlockChainContext
    {
        bool Preloaded { get; }

        BlockChain BlockChain { get; }

        IStore Store { get; }

        Swarm Swarm { get; }

        IBlockChainIndex Index { get; }

        INode Node { get; }
    }
}
