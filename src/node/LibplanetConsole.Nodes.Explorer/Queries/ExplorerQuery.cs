using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Explorer.Indexing;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Blocks;
using Libplanet.Types.Evidence;
using Libplanet.Types.Tx;
using LibplanetConsole.Explorer.GraphTypes;
using LibplanetConsole.Explorer.Interfaces;

namespace LibplanetConsole.Explorer.Queries
{
    public class ExplorerQuery : ObjectGraphType
    {
        private static IBlockChainContext? _chainContext;

        public ExplorerQuery(IBlockChainContext chainContext)
        {
            _chainContext = chainContext;
            Field<BlockQuery>("blockQuery", resolve: context => new { });
            Field<TransactionQuery>("transactionQuery", resolve: context => new { });
            Field<StateQuery>("stateQuery", resolve: context => chainContext.BlockChain);
            Field<NonNullGraphType<NodeStateType>>("nodeState", resolve: context => chainContext);
            Field<HelperQuery>("helperQuery", resolve: context => new { });
            Field<RawStateQuery>("rawStateQuery", resolve: context => chainContext.BlockChain);

            Name = "ExplorerQuery";
        }

        private static IBlockChainContext ChainContext => _chainContext!;

        private static BlockChain Chain => ChainContext.BlockChain;

        private static IStore Store => ChainContext.Store;

        private static IBlockChainIndex Index => ChainContext.Index;

        internal static IEnumerable<Block> ListBlocks(
            bool desc,
            long offset,
            long? limit,
            bool excludeEmptyTxs,
            Address? miner)
        {
            Block tip = Chain.Tip;
            long tipIndex = tip.Index;
            IStore store = ChainContext.Store;

            if (desc)
            {
                if (offset < 0)
                {
                    offset = tipIndex + offset + 1;
                }
                else
                {
                    offset = tipIndex - offset + 1 - (limit ?? 0);
                }
            }
            else
            {
                if (offset < 0)
                {
                    offset = tipIndex + offset + 1;
                }
            }

            var indexList = store.IterateIndexes(
                    Chain.Id,
                    (int)offset,
                    limit == null ? null : (int)limit)
                .Select((value, i) => new { i, value });

            if (desc)
            {
                indexList = indexList.Reverse();
            }

            foreach (var index in indexList)
            {
                Block block = store.GetBlock(index.value)
                    ?? throw new InvalidOperationException(
                        $"Could not find block with block hash {index.value} in store.");

                bool isMinerValid = miner is null || miner == block.Miner;
                bool isTxValid = !excludeEmptyTxs || block.Transactions.Any();
                if (!isMinerValid || !isTxValid)
                {
                    continue;
                }

                yield return block;
            }
        }

        internal static IEnumerable<Transaction> ListTransactions(
            Address? signer, bool desc, long offset, int? limit)
        {
            Block tip = Chain.Tip;
            long tipIndex = tip.Index;

            if (offset < 0)
            {
                offset = tipIndex + offset + 1;
            }

            if (tipIndex < offset || offset < 0)
            {
                yield break;
            }

            Block? block = Chain[desc ? tipIndex - offset : offset];
            while (!(block is null) && (limit is null || limit > 0))
            {
                foreach (var tx in desc ? block.Transactions.Reverse() : block.Transactions)
                {
                    if (IsValidTransaction(tx, signer))
                    {
                        yield return tx;
                        limit--;
                        if (limit <= 0)
                        {
                            break;
                        }
                    }
                }

                block = GetNextBlock(block, desc);
            }
        }

        internal static IEnumerable<Transaction> ListStagedTransactions(
            Address? signer, bool desc, int offset, int? limit)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset),
                    $"{nameof(ListStagedTransactions)} doesn't support negative offset.");
            }

            var stagedTxs = Chain.StagePolicy.Iterate(Chain)
                .Where(tx => IsValidTransaction(tx, signer))
                .Skip(offset);

            stagedTxs = desc ? stagedTxs.OrderByDescending(tx => tx.Timestamp)
                : stagedTxs.OrderBy(tx => tx.Timestamp);

            stagedTxs = stagedTxs.TakeWhile((tx, index) => limit is null || index < limit);

            return stagedTxs;
        }

        internal static IEnumerable<EvidenceBase> ListPendingEvidence(
            bool desc, int offset, int? limit)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset),
                    $"{nameof(ListPendingEvidence)} doesn't support negative offset.");
            }

            var blockChain = Chain;
            var comparer = desc ? EvidenceIdComparer.Descending : EvidenceIdComparer.Ascending;
            var evidence = blockChain.GetPendingEvidence()
                                      .Skip(offset)
                                      .Take(limit ?? int.MaxValue)
                                      .OrderBy(ev => ev.Id, comparer);

            return evidence;
        }

        internal static IEnumerable<EvidenceBase> ListCommitEvidence(
            BlockHash? blockHash, bool desc, int offset, int? limit)
        {
            var blockChain = Chain;
            var block = blockHash != null ? blockChain[blockHash.Value] : blockChain.Tip;
            var comparer = desc ? EvidenceIdComparer.Descending : EvidenceIdComparer.Ascending;
            var evidence = block.Evidence
                                 .Skip(offset)
                                 .Take(limit ?? int.MaxValue)
                                 .OrderBy(ev => ev.Id, comparer);

            return evidence;
        }

        internal static Block? GetBlockByHash(BlockHash hash) => Store.GetBlock(hash);

        internal static Block GetBlockByIndex(long index) => Chain[index];

        internal static Transaction GetTransaction(TxId id) => Chain.GetTransaction(id);

        internal static EvidenceBase GetEvidence(EvidenceId id) => Chain.GetCommittedEvidence(id);

        private static Block? GetNextBlock(Block block, bool desc)
        {
            if (desc && block.PreviousHash is { } prev)
            {
                return Chain[prev];
            }
            else if (!desc && block != Chain.Tip)
            {
                return Chain[block.Index + 1];
            }

            return null;
        }

        private static bool IsValidTransaction(
            Transaction tx,
            Address? signer)
        {
            if (signer is { } signerVal)
            {
                return tx.Signer.Equals(signerVal);
            }

            return true;
        }

        private static readonly byte[] _conversionTable =
        {
            48,  // '0'
            49,  // '1'
            50,  // '2'
            51,  // '3'
            52,  // '4'
            53,  // '5'
            54,  // '6'
            55,  // '7'
            56,  // '8'
            57,  // '9'
            97,  // 'a'
            98,  // 'b'
            99,  // 'c'
            100, // 'd'
            101, // 'e'
            102, // 'f'
        };

        internal static KeyBytes ToStateKey(Address address)
        {
            var addressBytes = address.ByteArray;
            byte[] buffer = new byte[addressBytes.Length * 2];
            for (int i = 0; i < addressBytes.Length; i++)
            {
                buffer[i * 2] = _conversionTable[addressBytes[i] >> 4];
                buffer[i * 2 + 1] = _conversionTable[addressBytes[i] & 0xf];
            }

            return new KeyBytes(buffer);
        }
    }
}
