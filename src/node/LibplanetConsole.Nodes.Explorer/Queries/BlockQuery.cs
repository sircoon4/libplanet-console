using System.Security.Cryptography;
using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Types.Blocks;
using LibplanetConsole.Common;
using LibplanetConsole.Explorer.GraphTypes;
using LibplanetConsole.Explorer.Interfaces;

namespace LibplanetConsole.Explorer.Queries
{
    public class BlockQuery : ObjectGraphType
    {
        public BlockQuery()
        {
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<BlockType>>>>(
                "blocks",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<BooleanGraphType>>
                    {
                        Name = "desc",
                        Description = "Whether to query blocks in descending order or not.",
                        DefaultValue = false,
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "offset",
                        Description = "The offset of the first queried block.",
                        DefaultValue = 0,
                    },
                    new QueryArgument<IntGraphType>
                    {
                        Name = "limit",
                        Description =
                            "The maximum number of blocks to return.  This limits the " +
                            "offset index range to query, not the result, i.e. excluded " +
                            "blocks due to a block being empty or not matching the miner " +
                            "(if specified in other arguments) are still counted.",
                    },
                    new QueryArgument<NonNullGraphType<BooleanGraphType>>
                    {
                        Name = "excludeEmptyTxs",
                        Description =
                            "Whether to include empty blocks with no transactions or not. " +
                            "Default is set to false, i.e. to return empty blocks.",
                        DefaultValue = false,
                    },
                    new QueryArgument<AddressType>
                    {
                        Name = "miner",
                        Description =
                            "If not null, returns blocks only by mined by the address given. " +
                            "Default is set to null.",
                    }
                ),
                resolve: context =>
                {
                    bool desc = context.GetArgument<bool>("desc");
                    long offset = context.GetArgument<long>("offset");
                    int? limit = context.GetArgument<int?>("limit", null);
                    bool excludeEmptyTxs = context.GetArgument<bool>("excludeEmptyTxs");
                    Address? miner = context.GetArgument<Address?>("miner", null);
                    return ExplorerQuery.ListBlocks(desc, offset, limit, excludeEmptyTxs, miner);
                }
            );

            Field<BlockType>(
                "block",
                arguments: new QueryArguments(
                    new QueryArgument<IdGraphType> { Name = "hash" },
                    new QueryArgument<IdGraphType> { Name = "index" }
                ),
                resolve: context =>
                {
                    string hash = context.GetArgument<string>("hash");
                    long? index = context.GetArgument<long?>("index", null);

                    if (!(hash is null ^ index is null))
                    {
                        throw new GraphQL.ExecutionError(
                            "The parameters hash and index are mutually exclusive; " +
                            "give only one at a time.");
                    }

                    if (hash is { } nonNullHash)
                    {
                        return ExplorerQuery.GetBlockByHash(BlockHash.FromString(nonNullHash));
                    }

                    if (index is { } nonNullIndex)
                    {
                        return ExplorerQuery.GetBlockByIndex(nonNullIndex);
                    }

                    throw new GraphQL.ExecutionError("Unexpected block query");
                }
            );

            Name = "BlockQuery";
        }
    }
}
