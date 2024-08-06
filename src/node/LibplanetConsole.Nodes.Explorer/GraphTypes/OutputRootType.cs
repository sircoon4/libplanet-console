using GraphQL.Types;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class OutputRootType : ObjectGraphType<OutputRoot>
    {
        public OutputRootType()
        {
            Field<LongGraphType>(
                nameof(OutputRoot.BlockIndex),
                description: "Recent block index.",
                resolve: context => context.Source.BlockIndex);

            Field<HashDigestSHA256Type>(
                nameof(OutputRoot.StateRootHash),
                description: "Recent state root hash.",
                resolve: context => context.Source.StateRootHash);

            Field<HashDigestSHA256Type>(
                nameof(OutputRoot.StorageRootHash),
                description: "Recent storage root hash of the withdrawal account.",
                resolve: context => context.Source.StorageRootHash);
        }
    }
}
