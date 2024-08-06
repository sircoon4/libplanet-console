using System.Collections.Generic;
using System.Security.Cryptography;
using Libplanet.Common;

namespace LibplanetConsole.Explorer.GraphTypes
{
    public class OutputRoot
    {
        public OutputRoot(
            long blockIndex,
            HashDigest<SHA256> stateRootHash,
            HashDigest<SHA256> storageRootHash)
        {
            BlockIndex = blockIndex;
            StateRootHash = stateRootHash;
            StorageRootHash = storageRootHash;
        }

        public long BlockIndex { get; private set; }

        public HashDigest<SHA256> StateRootHash { get; private set; }

        public HashDigest<SHA256> StorageRootHash { get; private set; }
    }
}
