using System;
using System.Text;
using Newtonsoft.Json;

namespace Hatgame.Multiplayer
{
    public class NetworkMessageSerializer : INetworkMessageSerializer
    {
        public object Deserialize(ReadOnlySpan<byte> buffer, Type messageType)
        {
            var data = Encoding.UTF8.GetString(buffer);
            var deserializedData = JsonConvert.DeserializeObject(data, messageType);
            return deserializedData;
        }

        public void Serialize(object objectTiSerialize, ref byte[] buffer, out int numberOfBytes)
        {
            var serializedData = JsonConvert.SerializeObject(objectTiSerialize);
            numberOfBytes = Encoding.UTF8.GetBytes(serializedData, buffer);
        }
    }
}
