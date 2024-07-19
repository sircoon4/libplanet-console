using GraphQL;
using GraphQL.Types;
using Libplanet.Blockchain;
using Libplanet.Common;
using Libplanet.Types.Tx;
using LibplanetConsole.Common.Actions;
using LibplanetConsole.Explorer.GraphTypes;
using LibplanetConsole.Explorer.Interfaces;
using LibplanetConsole.Nodes;

namespace LibplanetConsole.Explorer.Mutations
{
    public class TransactionMutation : ObjectGraphType
    {
        private readonly IBlockChainContext _context;

        public TransactionMutation(IBlockChainContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            Field<TransactionType>(
                "stage",
                description: "Stage a transaction to current chain",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "payload",
                        #pragma warning disable MEN002
                        Description = "The hexadecimal string of the serialized transaction to stage.",
                        #pragma warning restore MEN002
                    }),
                resolve: context =>
                {
                    BlockChain chain = _context.BlockChain;
                    byte[] payload = ByteUtil.ParseHex(context.GetArgument<string>("payload"));
                    Transaction tx = Transaction.Deserialize(payload);
                    if (!chain.StageTransaction(tx))
                    {
                        throw new ExecutionError(
                            "Failed to stage the given transaction;" +
                            "it may be already expired or ignored.");
                    }

                    return tx;
                });

            Field<StringGraphType>(
                "simpleStringStage",
                description: "Stage a simple string transaction to current chain",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "simpleString",
                        #pragma warning disable MEN002
                        Description = "A simple string to stage.",
                        #pragma warning restore MEN002
                    }),
                resolve: context =>
                {
                    using (CancellationTokenSource cts = new())
                    {
                        INode node = _context.Node;

                        string simple = context.GetArgument<string>("simpleString");

                        var action = new StringAction { Value = simple };

                        node.AddTransactionAsync([action], cts.Token).Wait();

                        return simple;
                    }
                });
        }
    }
}
