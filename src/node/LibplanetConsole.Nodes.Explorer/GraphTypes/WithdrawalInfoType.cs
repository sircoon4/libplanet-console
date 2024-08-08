using GraphQL.Types;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class WithdrawalInfoType : ObjectGraphType<WithdrawalInfo>
    {
        public WithdrawalInfoType()
        {
            Field<NonNullGraphType<LongGraphType>>(
                nameof(WithdrawalInfo.Nonce),
                description: "The nonce of the withdrawal.",
                resolve: context => context.Source.Nonce);

            Field<NonNullGraphType<AddressType>>(
                nameof(WithdrawalInfo.From),
                description: "The address of the sender.",
                resolve: context => context.Source.From);

            Field<NonNullGraphType<AddressType>>(
                nameof(WithdrawalInfo.To),
                description: "The address of the recipient.",
                resolve: context => context.Source.To);

            Field<NonNullGraphType<LongGraphType>>(
                nameof(WithdrawalInfo.Amount),
                description: "The amount of the withdrawal.",
                resolve: context => context.Source.Amount);
        }
    }
}
