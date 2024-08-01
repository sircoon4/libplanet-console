using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace LibplanetConsole.Common.Actions;

[ActionType(ActionTypeValue)]
public sealed class MintAction : ActionBase
{
    private const string ActionTypeValue = "mint_action";

    public MintAction(Address recipient, FungibleAssetValue amount)
    {
        Recipient = recipient;
        Amount = amount;
    }

    public MintAction()
    {
    }

    public Address Recipient { get; set; }

    public FungibleAssetValue Amount { get; set; }

    protected override Dictionary OnInitialize(Dictionary values)
    {
        return base.OnInitialize(values)
            .Add("recipient", Recipient.ToByteArray())
            .Add("amount", Amount.Serialize());
    }

    protected override void OnLoadPlainValue(Dictionary values)
    {
        base.OnLoadPlainValue(values);
        Recipient = new Address((Binary)values["recipient"]);
        Amount = new FungibleAssetValue(values["amount"]);
    }

    protected override IWorld OnExecute(IActionContext context)
    {
        IActionContext ctx = context;
        var states = ctx.PreviousState;
        return states.MintAsset(context, Recipient, Amount);
    }
}
