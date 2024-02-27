using System;

namespace Hatgame.Multiplayer
{
    public interface INetworkMessageSerializer
    {
        void Serialize(object objectTiSerialize, ref byte[] buffer, out int numberOfBytes);

        object Deserialize(ReadOnlySpan<byte> buffer, Type messageType);
    }
}
