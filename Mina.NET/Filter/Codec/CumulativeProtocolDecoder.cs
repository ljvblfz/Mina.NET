using System;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// A <see cref="IProtocolDecoder"/> that cumulates the content of received buffers to a
    /// cumulative buffer to help users implement decoders. 
    /// </summary>
    public abstract class CumulativeProtocolDecoder : ProtocolDecoderAdapter
    {
        private readonly AttributeKey _buffer = new AttributeKey(typeof(CumulativeProtocolDecoder), "buffer");

        /// <summary>
        /// </summary>
        protected CumulativeProtocolDecoder()
        {
        }

        /// <inheritdoc/>
        public override void Decode(IOSession session, IOBuffer input, IProtocolDecoderOutput output)
        {
            if (!session.TransportMetadata.HasFragmentation)
            {
                while (input.HasRemaining)
                {
                    if (!DoDecode(session, input, output))
                    {
                        break;
                    }
                }

                return;
            }

            var usingSessionBuffer = true;
            var buf = session.GetAttribute<IOBuffer>(_buffer);

            // If we have a session buffer, append data to that; otherwise
            // use the buffer read from the network directly.
            if (buf == null)
            {
                buf = input;
                usingSessionBuffer = false;
            }
            else
            {
                var appended = false;
                // Make sure that the buffer is auto-expanded.
                if (buf.AutoExpand)
                {
                    try
                    {
                        buf.Put(input);
                        appended = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // A user called derivation method (e.g. slice()),
                        // which disables auto-expansion of the parent buffer.
                    }
                    catch (OverflowException)
                    {
                        // A user disabled auto-expansion.
                    }
                }

                if (appended)
                {
                    buf.Flip();
                }
                else
                {
                    // Reallocate the buffer if append operation failed due to
                    // derivation or disabled auto-expansion.
                    buf.Flip();

                    var newBuf = IOBuffer.Allocate(buf.Remaining + input.Remaining);
                    newBuf.AutoExpand = true;
                    newBuf.Order = buf.Order;
                    newBuf.Put(buf);
                    newBuf.Put(input);
                    newBuf.Flip();
                    buf = newBuf;

                    // Update the session attribute.
                    session.SetAttribute(_buffer, buf);
                }
            }

            while (true)
            {
                var oldPos = buf.Position;
                var decoded = DoDecode(session, buf, output);
                if (decoded)
                {
                    if (buf.Position == oldPos)
                    {
                        throw new InvalidOperationException("DoDecode() can't return true when buffer is not consumed.");
                    }
                    if (!buf.HasRemaining)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            // if there is any data left that cannot be decoded, we store
            // it in a buffer in the session and next time this decoder is
            // invoked the session buffer gets appended to
            if (buf.HasRemaining)
            {
                if (usingSessionBuffer && buf.AutoExpand)
                {
                    buf.Compact();
                }
                else
                {
                    StoreRemainingInSession(buf, session);
                }
            }
            else
            {
                if (usingSessionBuffer)
                {
                    RemoveSessionBuffer(session);
                }
            }
        }

        /// <summary>
        /// Implement this method to consume the specified cumulative buffer and decode its content into message(s).
        /// </summary>
        /// <param name="session"></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns>
        /// true if and only if there's more to decode in the buffer
        /// and you want to have DoDecode method invoked again.
        /// Return false if remaining data is not enough to decode,
        /// then this method will be invoked again when more data is cumulated.
        /// </returns>
        protected abstract bool DoDecode(IOSession session, IOBuffer input, IProtocolDecoderOutput output);

        private void RemoveSessionBuffer(IOSession session)
        {
            session.RemoveAttribute(_buffer);
        }

        private void StoreRemainingInSession(IOBuffer buf, IOSession session)
        {
            var remainingBuf = IOBuffer.Allocate(buf.Capacity);
            remainingBuf.AutoExpand = true;

            remainingBuf.Order = buf.Order;
            remainingBuf.Put(buf);

            session.SetAttribute(_buffer, remainingBuf);
        }
    }
}
