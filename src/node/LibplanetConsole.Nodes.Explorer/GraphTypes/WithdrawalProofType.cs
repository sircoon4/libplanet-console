using System.Security.Cryptography;
using GraphQL.Types;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class WithdrawalProofType : ObjectGraphType<WithdrawalProof>
    {
        public WithdrawalProofType()
        {
            Field<IValueType>(
                nameof(WithdrawalProof.WithdrawalInfo),
                description: "Withdrawal information.",
                resolve: context => context.Source.WithdrawalInfo);

            Field<IValueType>(
                nameof(WithdrawalProof.Proof),
                description: "The inclusion proof of withdrawal transaction.",
                resolve: context => context.Source.Proof);
        }
    }
}
