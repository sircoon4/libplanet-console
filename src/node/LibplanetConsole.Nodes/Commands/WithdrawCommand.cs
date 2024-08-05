using System.ComponentModel.Composition;
using System.Numerics;
using JSSoft.Commands;
using Libplanet.Crypto;
using LibplanetConsole.Common.Actions;

namespace LibplanetConsole.Nodes.Commands;

[Export(typeof(ICommand))]
[method: ImportingConstructor]
[CommandSummary("Adds a transaction to withdraw ETH.")]
internal sealed class WithdrawCommand(IBlockChain blockChain) : CommandAsyncBase
{
    [CommandPropertyRequired]
    public required string Recipient { get; set; }

    [CommandPropertyRequired]
    public required int Amount { get; set; }

    protected override async Task OnExecuteAsync(
        CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        var recipient = new Address(Recipient);
        var amount = new BigInteger(Amount);
        var action = new WithdrawEthAction(recipient, amount);
        await blockChain.AddTransactionAsync([action], cancellationToken);
        await Out.WriteLineAsync($"{Recipient:S}: {Amount}");
    }
}
