using ProtoBuf;

namespace JoyEngine.Common.Messages
{
    [ProtoContract]
    public class QueueMessage : IoMessage
    {
        [ProtoMember(5)]
        public string Status;

        public QueueMessage()
        {
            //
        }

        public QueueMessage(IoMessage message)
        {
            From = message.From;
            To = message.To;
            ReplyTo = message.ReplyTo;
            Body = message.Body;
        }
    }
}
