using System.Collections.Generic;
using GraphQL.Types;
using Libplanet.Net;
using LibplanetConsole.Explorer.Interfaces;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class NodeStateType : ObjectGraphType<IBlockChainContext>
    {
        public NodeStateType()
        {
            Name = "NodeState";

            Field<NonNullGraphType<BooleanGraphType>>(
                name: "preloaded",
                resolve: context => context.Source.Preloaded
            );
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<BoundPeerType>>>>(
                name: "peers",
                resolve: context => context.Source.Swarm?.Peers ?? new List<BoundPeer>()
            );
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<BoundPeerType>>>>(
                name: "validators",
                resolve: context => context.Source.Swarm?.Validators ?? new List<BoundPeer>()
            );
        }
    }
}
