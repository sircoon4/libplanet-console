using System.Security.Cryptography;
using GraphQL.Types;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class InclusionProofType : ObjectGraphType<InclusionProof>
    {
        public InclusionProofType()
        {
            Field<HashDigestSHA256Type>(
                nameof(InclusionProof.StateRootHash),
                description: "The state's root hash containing the target key-value pair.",
                resolve: context => context.Source.StateRootHash);

            Field<IValueType>(
                nameof(InclusionProof.Proof),
                description: "The inclusion proof of the target key-value pair.",
                resolve: context => context.Source.Proof);

            Field<KeyBytesType>(
                nameof(InclusionProof.Key),
                description: "The key of the target key-value pair.",
                resolve: context => context.Source.Key);

            Field<IValueType>(
                nameof(InclusionProof.Value),
                description: "The value of the target key-value pair.",
                resolve: context => context.Source.Value);
        }
    }
}
