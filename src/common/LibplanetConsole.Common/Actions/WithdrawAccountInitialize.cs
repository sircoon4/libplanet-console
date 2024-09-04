using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Serilog;

namespace LibplanetConsole.Common.Actions;

[ActionType(ActionTypeValue)]
public sealed class WithdrawAccountInitialize : ActionBase
{
    private const string ActionTypeValue = "withdraw_account_initialize";

    public WithdrawAccountInitialize()
    {
    }

    protected override IWorld OnExecute(IActionContext context)
    {
        Address withdrawAddress = AssetUtility.GetWithdrawalAccountAddress();

        var world = context.PreviousState;

        var account = world.GetAccount(withdrawAddress);
        world = world.SetAccount(withdrawAddress, account);

        return world;
    }
}
