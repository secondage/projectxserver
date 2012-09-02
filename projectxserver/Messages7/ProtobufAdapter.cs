using System;
using System.Collections.Generic;
using System.Text;
using Beetle;
using System.Reflection;
namespace ProjectXServer.Messages
{
    public class ProtobufAdapter:IMessage
    //public class ProtobufAdapter : Beetle.ProtoBufAdapter.MessageAdapter
    {
        public object Message
        {
            get;
            set;
        }
        public static void LoadMessage(Assembly assembly)
        {
            Beetle.ProtoBufAdapter.MessageAdapter.LoadMessage(assembly);
        }
        public static void Send(Beetle.TcpChannel channel, object msg)
        {
            ProtobufAdapter adapter = new ProtobufAdapter();
            adapter.Message = msg;
            channel.Send(adapter);
            //Beetle.ProtoBufAdapter.MessageAdapter.Send(channel, msg);
        }
        public void Load(Beetle.BufferReader reader)
        {
            string type = reader.ReadString();
            //Beetle.ByteArraySegment segment = mArrayPool.Pop();
            byte[] data = reader.ReadByteArray();
            using (System.IO.Stream stream = new System.IO.MemoryStream(data, 0, data.Length))
            {
                Message = ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(stream, null, Type.GetType(type));
            }
        }
        public void Save(Beetle.BufferWriter writer)
        {
            writer.Write(Message.GetType().FullName);
            byte[] data;
            using (System.IO.Stream stream = new System.IO.MemoryStream())
            {
                ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(stream, Message);
                data = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(data, 0, data.Length);
            }
            writer.Write(data);
        }
        
    }
}
