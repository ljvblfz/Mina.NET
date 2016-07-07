using System;
using System.Runtime.Serialization;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Serialization
{
    /// <summary>
    /// A <see cref="IProtocolEncoder"/> which serializes <code>Serializable</code> objects,
    /// using <see cref="IOBuffer.PutObject(object)"/>.
    /// </summary>
    public class ObjectSerializationEncoder : ProtocolEncoderAdapter
    {
        private int _maxObjectSize = int.MaxValue; // 2GB

        /// <summary>
        /// Gets or sets the allowed maximum size of the encoded object.
        /// If the size of the encoded object exceeds this value, this encoder
        /// will throw a <see cref="ArgumentException"/>.  The default value
        /// is <see cref="int.MaxValue"/>.
        /// </summary>
        public int MaxObjectSize
        {
            get { return _maxObjectSize; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("MaxObjectSize should be larger than zero.", nameof(value));
                }
                _maxObjectSize = value;
            }
        }

        /// <inheritdoc/>
        public override void Encode(IOSession session, object message, IProtocolEncoderOutput output)
        {
            if (!message.GetType().IsSerializable)
            {
                throw new SerializationException(message.GetType() + " is not serializable.");
            }

            var buf = IOBuffer.Allocate(64);
            buf.AutoExpand = true;
            buf.PutInt32(0);
            buf.PutObject(message);

            var objectSize = buf.Position - 4;
            if (objectSize > _maxObjectSize)
            {
                throw new ArgumentException(string.Format("The encoded object is too big: {0} (> {1})",
                    objectSize, _maxObjectSize), nameof(message));
            }

            buf.PutInt32(0, objectSize);
            buf.Flip();
            output.Write(buf);
        }
    }
}
