using System.Diagnostics;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Store.Trie.Nodes;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using LibplanetConsole.Common;
using LibplanetConsole.Explorer.GraphTypes;

namespace LibplanetConsole.Explorer.Queries;

public class StateQuery : ObjectGraphType<IBlockChainStates>
{
    public StateQuery()
    {
        Name = "StateQuery";

        Field<NonNullGraphType<WorldStateType>>(
            "world",
            arguments: new QueryArguments(
                new QueryArgument<BlockHashType> { Name = "blockHash" },
                new QueryArgument<HashDigestType<SHA256>> { Name = "stateRootHash" }
            ),
            resolve: ResolveWorldState
        );
        Field<NonNullGraphType<ListGraphType<BencodexValueType>>>(
            "states",
            description: "Retrieves states from the legacy account.",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<ListGraphType<NonNullGraphType<AddressType>>>>
                    { Name = "addresses" },
                new QueryArgument<IdGraphType> { Name = "offsetBlockHash" },
                new QueryArgument<HashDigestSHA256Type> { Name = "offsetStateRootHash" }
            ),
            resolve: ResolveStates
        );
        Field<NonNullGraphType<HashDigestSHA256Type>>(
            "withdrawalState",
            description: "Retrieves states from withdrawal account.",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<HashDigestSHA256Type>> { Name = "offsetStateRootHash" }
            ),
            resolve: ResolveWithdrawalState
        );
        Field<NonNullGraphType<FungibleAssetValueType>>(
            "balance",
            description: "Retrieves balance from the legacy account.",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<AddressType>> { Name = "owner" },
                new QueryArgument<NonNullGraphType<CurrencyInputType>> { Name = "currency" },
                new QueryArgument<IdGraphType> { Name = "offsetBlockHash" },
                new QueryArgument<HashDigestSHA256Type> { Name = "offsetStateRootHash" }
            ),
            resolve: ResolveBalance
        );
        Field<FungibleAssetValueType>(
            "totalSupply",
            description: "Retrieves total supply from the legacy account.",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CurrencyInputType>> { Name = "currency" },
                new QueryArgument<IdGraphType> { Name = "offsetBlockHash" },
                new QueryArgument<HashDigestSHA256Type> { Name = "offsetStateRootHash" }
            ),
            resolve: ResolveTotalSupply
        );
        Field<ListGraphType<NonNullGraphType<ValidatorType>>>(
            "validators",
            description: "Retrieves validator set from the legacy account.",
            arguments: new QueryArguments(
                new QueryArgument<IdGraphType> { Name = "offsetBlockHash" },
                new QueryArgument<HashDigestSHA256Type> { Name = "offsetStateRootHash" }
            ),
            resolve: ResolveValidatorSet
        );
        Field<NonNullGraphType<IValueType>>(
            "proof",
            description: "Retrieves inclusion proof of the given address and value.",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<AddressType>>
                {
                    Name = "address",
                },
                new QueryArgument<NonNullGraphType<StringGraphType>>
                {
                    Name = "value",
                },
                new QueryArgument<NonNullGraphType<HashDigestSHA256Type>>
                {
                    Name = "stateRootHash",
                }
            ),
            resolve: ResolveInclusionProof
        );
        Field<OutputRootType>(
            "outputRoot",
            arguments: new QueryArguments(
                new QueryArgument<IdGraphType>
                {
                    Name = "index",
                    Description = "if empty, latest block will be returned",
                }),
            resolve: ResolveOutputRootState);
        Field<WithdrawalProofType>(
            "withdrawalProof",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<HashDigestSHA256Type>>
                {
                    Name = "storageRootHash",
                },
                new QueryArgument<NonNullGraphType<IdGraphType>>
                {
                    Name = "txId",
                    Description = "transaction id.",
                }
            ),
            resolve: ResolveWithdrawalProof);
    }

    private static object ResolveWorldState(IResolveFieldContext<IBlockChainStates> context)
    {
        BlockHash? blockHash = context.GetArgument<BlockHash?>("blockHash");
        HashDigest<SHA256>? stateRootHash =
            context.GetArgument<HashDigest<SHA256>?>("stateRootHash");

        Codec codec = new Codec();

        switch (blockhash: blockHash, srh: stateRootHash)
        {
            case (blockhash: not null, srh: not null):
                throw new ExecutionError(
                    "blockHash and stateRootHash cannot be specified at the same time."
                );
            case (blockhash: null, srh: null):
                throw new ExecutionError(
                    "Either blockHash or stateRootHash must be specified."
                );
            case (blockhash: not null, _):
                var trie = (MerkleTrie)context.Source.GetWorldState((BlockHash)blockHash).Trie;
                foreach (var (key, value) in trie.IterateValues())
                {
                    KeyBytes keyBytes = key;
                    var keyString = System.Text.Encoding.UTF8.GetString(keyBytes.ToByteArray());
                    IValue bencodexValue = value;
                    string bencodexString;
                    if (bencodexValue is Binary)
                    {
                        bencodexString = ((Binary)bencodexValue).ToHex();
                    }
                    else
                    {
                        bencodexString = bencodexValue.Inspect();
                    }

                    Debug.WriteLine($"{keyString}: {bencodexString}");
                }
                return context.Source.GetWorldState((BlockHash)blockHash);
            case (_, srh: not null):
                var trie2 = (MerkleTrie)context.Source.GetWorldState(stateRootHash).Trie;
                foreach (var item in trie2.IterateNodes())
                {
                    var (nibbles, _) = item;
                    if (item.Node is ValueNode valueNode)
                    {
                        var kb = nibbles.ToKeyBytes();
                        string keyString = System.Text.Encoding.UTF8.GetString(kb.ToByteArray());
                        Debug.WriteLine($"key: {keyString}");

                        if (valueNode.Value is Binary binary)
                        {
                            Debug.WriteLine($"value: {binary.ToHex()}");
                            string bencodedString = ByteUtil.Hex(codec.Encode(binary));
                            Debug.WriteLine($"bencoded: {bencodedString}");
                        }
                        else
                        {
                            Debug.WriteLine($"value: {valueNode.Value.Inspect()}");
                        }
                    }
                }
                return context.Source.GetWorldState(stateRootHash);
        }
    }

    private static object? ResolveStates(IResolveFieldContext<IBlockChainStates> context)
    {
        Address[] addresses = context.GetArgument<Address[]>("addresses");
        BlockHash? offsetBlockHash =
            context.GetArgument<string?>("offsetBlockHash") is { } blockHashString
                ? BlockHash.FromString(blockHashString)
                : null;
        HashDigest<SHA256>? offsetStateRootHash = context
            .GetArgument<HashDigest<SHA256>?>("offsetStateRootHash");

        switch (blockhash: offsetBlockHash, srh: offsetStateRootHash)
        {
            case (blockhash: not null, srh: not null):
                throw new ExecutionError(
                    "offsetBlockHash and offsetStateRootHash cannot be specified at the same time."
                );
            case (blockhash: null, srh: null):
                throw new ExecutionError(
                    "Either offsetBlockHash or offsetStateRootHash must be specified."
                );
            case (blockhash: not null, _):
            {
                return context.Source
                    .GetWorldState((BlockHash)offsetBlockHash)
                    .GetAccountState(ReservedAddresses.LegacyAccount)
                    .GetStates(addresses);
            }

            case (_, srh: not null):
                return context.Source
                    .GetWorldState(offsetStateRootHash)
                    .GetAccountState(ReservedAddresses.LegacyAccount)
                    .GetStates(addresses);
        }
    }

    private static object? ResolveWithdrawalState(IResolveFieldContext<IBlockChainStates> context)
    {
        HashDigest<SHA256> offsetStateRootHash = context
            .GetArgument<HashDigest<SHA256>>("offsetStateRootHash");

        HashDigest<SHA256>? storageRootHash = null;
        var trie = (MerkleTrie)context.Source.GetWorldState(offsetStateRootHash).Trie;
        Address withdrawAccountAddress = AssetUtility.GetWithdrawalAccountAddress();
        var key = ExplorerQuery.ToStateKey(withdrawAccountAddress);
        if (trie.Get(key) is { } storageRootHashValue)
        {
            storageRootHash = new HashDigest<SHA256>(
                ((Binary)storageRootHashValue).ToByteArray());
        }

        if (storageRootHash is { } nonNullStorageRootHash)
        {
            return nonNullStorageRootHash;
        }
        else
        {
            throw new GraphQL.ExecutionError("No storage root hash found.");
        }
    }

    private static object ResolveBalance(IResolveFieldContext<IBlockChainStates> context)
    {
        Address owner = context.GetArgument<Address>("owner");
        Currency currency = context.GetArgument<Currency>("currency");
        BlockHash? offsetBlockHash =
            context.GetArgument<string?>("offsetBlockHash") is { } blockHashString
                ? BlockHash.FromString(blockHashString)
                : null;
        HashDigest<SHA256>? offsetStateRootHash = context
            .GetArgument<HashDigest<SHA256>?>("offsetStateRootHash");

        switch (blockhash: offsetBlockHash, srh: offsetStateRootHash)
        {
            case (blockhash: not null, srh: not null):
                throw new ExecutionError(
                    "offsetBlockHash and offsetStateRootHash cannot be specified at the same time."
                );
            case (blockhash: null, srh: null):
                throw new ExecutionError(
                    "Either offsetBlockHash or offsetStateRootHash must be specified."
                );
            case (blockhash: not null, _):
            {
                return context.Source
                    .GetWorldState((BlockHash)offsetBlockHash)
                    .GetBalance(owner, currency);
            }

            case (_, srh: not null):
                return context.Source
                    .GetWorldState(offsetStateRootHash)
                    .GetBalance(owner, currency);
        }
    }

    private static object? ResolveTotalSupply(IResolveFieldContext<IBlockChainStates> context)
    {
        Currency currency = context.GetArgument<Currency>("currency");
        BlockHash? offsetBlockHash =
            context.GetArgument<string?>("offsetBlockHash") is { } blockHashString
                ? BlockHash.FromString(blockHashString)
                : null;
        HashDigest<SHA256>? offsetStateRootHash = context
            .GetArgument<HashDigest<SHA256>?>("offsetStateRootHash");

        switch (blockhash: offsetBlockHash, srh: offsetStateRootHash)
        {
            case (blockhash: not null, srh: not null):
                throw new ExecutionError(
                    "offsetBlockHash and offsetStateRootHash cannot be specified at the same time."
                );
            case (blockhash: null, srh: null):
                throw new ExecutionError(
                    "Either offsetBlockHash or offsetStateRootHash must be specified."
                );
            case (blockhash: not null, _):
                return context.Source
                    .GetWorldState((BlockHash)offsetBlockHash)
                    .GetTotalSupply(currency);
            case (_, srh: not null):
                return context.Source
                    .GetWorldState(offsetStateRootHash)
                    .GetTotalSupply(currency);
        }
    }

    private static object? ResolveValidatorSet(IResolveFieldContext<IBlockChainStates> context)
    {
        BlockHash? offsetBlockHash =
            context.GetArgument<string?>("offsetBlockHash") is { } blockHashString
                ? BlockHash.FromString(blockHashString)
                : null;
        HashDigest<SHA256>? offsetStateRootHash = context
            .GetArgument<HashDigest<SHA256>?>("offsetStateRootHash");

        switch (blockhash: offsetBlockHash, srh: offsetStateRootHash)
        {
            case (blockhash: not null, srh: not null):
                throw new ExecutionError(
                    "offsetBlockHash and offsetStateRootHash cannot be specified at the same time."
                );
            case (blockhash: null, srh: null):
                throw new ExecutionError(
                    "Either offsetBlockHash or offsetStateRootHash must be specified."
                );
            case (blockhash: not null, _):
                return context.Source
                    .GetWorldState((BlockHash)offsetBlockHash)
                    .GetValidatorSet().Validators;
            case (_, srh: not null):
                return context.Source
                    .GetWorldState(offsetStateRootHash)
                    .GetValidatorSet().Validators;
        }
    }

    private static object? ResolveInclusionProof(IResolveFieldContext<IBlockChainStates> context)
    {
        var codec = new Codec();
        Address address = context.GetArgument<Address>("address");
        IValue value = codec.Decode(
            ByteUtil.ParseHex(
                context.GetArgument<string>("value")));
        HashDigest<SHA256> stateRootHash = context.GetArgument<HashDigest<SHA256>>("stateRootHash");

        var trie = (MerkleTrie)context.Source.GetWorldState(stateRootHash).Trie;

        foreach (var item in trie.IterateNodes())
        {
            var (nibbles, node) = item;
            if (nibbles.Length == 0)
            {
                continue;
            }

            Debug.WriteLine($"Node key: {nibbles.Hex}");
            IValue nodeValue = node.ToBencodex();
            string nodeString = nodeValue.Inspect();
            Debug.WriteLine($"Value node: {nodeString}");
        }

        var key = ExplorerQuery.ToStateKey(address);
        var proof = trie.GenerateProof(key, value);
        var bencodedProof = new List();

        foreach (var node in proof)
        {
            bencodedProof = bencodedProof.Add(node.ToBencodex());
        }

        return (IValue)bencodedProof;
    }

    private static object ResolveOutputRootState(IResolveFieldContext<IBlockChainStates> context)
    {
        long? index = context.GetArgument<long?>("index", null);

        Block block;
        if (index is { } nonNullIndex)
        {
            block = ExplorerQuery.GetBlockByIndex(nonNullIndex);
        }
        else
        {
            var blocks = ExplorerQuery.ListBlocks(true, 0, 1, false, null);

            if (!blocks.Any())
            {
                throw new GraphQL.ExecutionError("No blocks found.");
            }
            else if (blocks.Count() > 1)
            {
                throw new GraphQL.ExecutionError(
                    "Unexpected multiple blocks returned from the query.");
            }

            block = blocks.First();
        }

        var blockIndex = block.Index;
        var stateRootHash = block.StateRootHash;

        HashDigest<SHA256>? storageRootHash = null;
        var trie = (MerkleTrie)context.Source.GetWorldState(stateRootHash).Trie;
        Address withdrawAccountAddress = AssetUtility.GetWithdrawalAccountAddress();
        var key = ExplorerQuery.ToStateKey(withdrawAccountAddress);
        if (trie.Get(key) is { } storageRootHashValue)
        {
            storageRootHash = new HashDigest<SHA256>(
                ((Binary)storageRootHashValue).ToByteArray());
        }

        if (storageRootHash is { } nonNullStorageRootHash)
        {
            return new OutputRoot(blockIndex, stateRootHash, nonNullStorageRootHash);
        }
        else
        {
            throw new GraphQL.ExecutionError("No storage root hash found.");
        }
    }

    private static object ResolveWithdrawalProof(IResolveFieldContext<IBlockChainStates> context)
    {
        HashDigest<SHA256> storageRootHash =
            context.GetArgument<HashDigest<SHA256>>("storageRootHash");
        var txId = new TxId(ByteUtil.ParseHex(context.GetArgument<string>("txId")));

        var trie = (MerkleTrie)context.Source.GetWorldState(storageRootHash).Trie;
        var key = ExplorerQuery.ToStateKey(AssetUtility.GetWithdrawalDataAddress(txId));
        var value = trie.Get(key);

        if (value is null)
        {
            throw new GraphQL.ExecutionError("No withdrawal data found.");
        }

        var proofKey = ExplorerQuery.ToStateKey(
            new Address(((Dictionary)value).GetValue<Binary>("hash")));
        var proofs = trie.GenerateProof(proofKey, new Bencodex.Types.Boolean(true));
        var proof = new List();

        foreach (var item in proofs)
        {
            proof = proof.Add(item.ToBencodex());
        }

        var withdrawalInfo = new WithdrawalInfo(
            ((Dictionary)value).GetValue<Integer>("nonce"),
            new Address(((Dictionary)value).GetValue<Binary>("from")),
            new Address(((Dictionary)value).GetValue<Binary>("to")),
            ((Dictionary)value).GetValue<Integer>("amount"));

        return new WithdrawalProof(withdrawalInfo, proof);
    }
}
