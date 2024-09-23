﻿using System.Reflection;
using Libplanet.Blockchain;
using Libplanet.Explorer.Indexing;
using Libplanet.Net;
using Libplanet.Store;
using Microsoft.Extensions.DependencyInjection;

namespace LibplanetConsole.Nodes.Explorer;

internal sealed class BlockChainContext(INode node) : IBlockChainContext
{
    public bool Preloaded => false;

    public BlockChain BlockChain => node.GetRequiredService<BlockChain>();

#pragma warning disable S3011 // Reflection should not be used to increase accessibility ...
    public IStore Store
    {
        get
        {
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            var propertyInfo = typeof(BlockChain).GetProperty("Store", bindingFlags) ??
                throw new InvalidOperationException("Store property not found.");
            if (propertyInfo.GetValue(BlockChain) is IStore store)
            {
                return store;
            }

            throw new InvalidOperationException("Store property is not IStore.");
        }
    }
#pragma warning restore S3011

    public Swarm Swarm => node.GetRequiredService<Swarm>();

    public IBlockChainIndex Index => throw new NotSupportedException();

    public INode Node => node;
}
