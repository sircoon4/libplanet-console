using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Bencodex.Types;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Libplanet.Blockchain;
using Libplanet.Store;
using Libplanet.Types.Tx;
using LibplanetConsole.Explorer.GraphTypes;
using LibplanetConsole.Explorer.Interfaces;

namespace LibplanetConsole.Explorer.Subscriptions
{
    public class ExplorerSubscription : ObjectGraphType
    {
        private readonly IBlockChainContext _chainContext;

        private readonly ISubject<Transaction> _transactionSubject = new Subject<Transaction>();

        public ExplorerSubscription(IBlockChainContext chainContext)
        {
            Name = "ExplorerSubscription";

            _chainContext = chainContext;

            AddField(new EventStreamFieldType
            {
                Name = "tx",
                Type = typeof(TxType),
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "actionType",
                        Description =
                            "A regular expression to filter transactions based on action type.",
                    }),
                Resolver = new FuncFieldResolver<Tx>(ResolveTx),
                Subscriber = new EventStreamResolver<Tx>(SubscribeTx),
            });
        }

        private Tx ResolveTx(IResolveFieldContext context)
        {
            return context.Source as Tx ?? throw new InvalidOperationException();
        }

        private IObservable<Tx> SubscribeTx(IResolveFieldContext context)
        {
            var chain = _chainContext.BlockChain
                        ?? throw new InvalidOperationException(
                            "BlockChain is not initialized yet.");
            var store = _chainContext.Store
                        ?? throw new InvalidOperationException("Store is not initialized yet.");
            var actionType = context.GetArgument<string>("actionType");

            return _transactionSubject.AsObservable()
                .Where(tx => tx.Actions.Any(rawAction =>
                {
                    if (rawAction is not Dictionary action || action["type_id"] is not Text typeId)
                    {
                        return false;
                    }

                    return Regex.IsMatch(typeId, actionType);
                }))
                .Select(transaction => new Tx
                {
                    Transaction = transaction,
                    TxResult = GetTxResult(transaction, store, chain),
                });
        }

        private OutputRoot? GetTxResult(Transaction transaction, IStore store, BlockChain chain)
        {
            if (store.GetFirstTxIdBlockHashIndex(transaction.Id) is not { } blockHash)
            {
                return null;
            }

            var txExecution = store.GetTxExecution(blockHash, transaction.Id) ??
                throw new InvalidOperationException("TxExecution not found.");
            var txExecutedBlock = chain[blockHash];
            return new TxResult(
                txExecution.Fail ? TxStatus.FAILURE : TxStatus.SUCCESS,
                txExecutedBlock.Index,
                txExecutedBlock.Hash.ToString(),
                txExecution.InputState,
                txExecution.OutputState,
                txExecution.ExceptionNames);
        }

        private sealed class Tx
        {
            public Transaction? Transaction { get; set; }

            public OutputRoot? TxResult { get; set; }
        }

        private sealed class TxType : ObjectGraphType<Tx>
        {
            public TxType()
            {
                Field<NonNullGraphType<TransactionType>>(nameof(Transaction));
                Field<TxResultType>(nameof(OutputRoot));
            }
        }
    }
}
