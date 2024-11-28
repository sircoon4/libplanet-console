using System.ComponentModel.Composition;
using JSSoft.Terminals;
using LibplanetConsole.Common;
using LibplanetConsole.Common.Actions;
using LibplanetConsole.Common.Extensions;
using LibplanetConsole.Frameworks;

namespace LibplanetConsole.Nodes.Executable.Tracers;

[Export(typeof(IApplicationService))]
[method: ImportingConstructor]
internal sealed class BlockChainEventTracer(IBlockChain blockChain)
    : IApplicationService, IDisposable
{
    public Task InitializeAsync(
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        blockChain.BlockAppended += Node_BlockAppended;
        return Task.CompletedTask;
    }

    void IDisposable.Dispose()
    {
        blockChain.BlockAppended -= Node_BlockAppended;
    }

    private void Node_BlockAppended(object? sender, BlockEventArgs e)
    {
        var blockInfo = e.BlockInfo;
        var hash = blockInfo.Hash;
        var miner = blockInfo.Miner;
        var message = $"Block #{blockInfo.Height} '{hash:S}' Appended by '{miner:S}'";
        Console.Out.WriteColoredLine(message, TerminalColorType.BrightGreen);

        if (sender is INode node) {
            Random random = new Random();
            int randTransactionNum = random.Next(0, 5);

            for (int i = 0; i < randTransactionNum; i++)
            {
                int randStringSize = random.Next(1, 32);
                string randomString = new string(Enumerable.Repeat(
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", randStringSize)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                using var cts = new CancellationTokenSource();
                var action = new StringAction { Value = randomString };
                node.AddTransactionAsync([action], cts.Token);
            }
        }
    }
}
