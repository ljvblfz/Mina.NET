using System;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.ErrorGenerating
{
    /// <summary>
    /// An <see cref="IOFilter"/> implementation generating random bytes and PDU modification in
    /// your communication streams.
    /// 
    /// It's quite simple to use:
    /// <code>ErrorGeneratingFilter egf = new ErrorGeneratingFilter();</code>
    /// For activate the change of some bytes in your <see cref="IOBuffer"/>, for a probability of 200 out
    /// of 1000 processed:
    /// <code>egf.ChangeByteProbability = 200;</code>
    /// For activate the insertion of some bytes in your <see cref="IOBuffer"/>, for a
    /// probability of 200 out of 1000:
    /// <code>egf.InsertByteProbability = 200;</code>
    /// And for the removing of some bytes :
    /// <code>egf.RemoveByteProbability = 200;</code>
    /// You can activate the error generation for write or read with the
    /// following methods :
    /// <code>egf.ManipulateReads = true;
    /// egf.ManipulateWrites = true;</code>
    /// </summary>
    public class ErrorGeneratingFilter : IOFilterAdapter
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(ErrorGeneratingFilter));

        private Random _rng = new Random();

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            if (ManipulateWrites)
            {
                // manipulate bytes
                var buf = writeRequest.Message as IOBuffer;
                if (buf != null)
                {
                    ManipulateIoBuffer(session, buf);
                    var buffer = InsertBytesToNewIoBuffer(session, buf);
                    if (buffer != null)
                    {
                        writeRequest = new DefaultWriteRequest(buffer, writeRequest.Future);
                    }
                    // manipulate PDU
                }
                else
                {
                    if (DuplicatePduProbability > _rng.Next())
                    {
                        nextFilter.FilterWrite(session, writeRequest);
                    }

                    if (ResendPduLasterProbability > _rng.Next())
                    {
                        // store it somewhere and trigger a write execution for
                        // later
                        // TODO
                    }
                    if (RemovePduProbability > _rng.Next())
                    {
                        return;
                    }
                }
            }

            base.FilterWrite(nextFilter, session, writeRequest);
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            if (ManipulateReads)
            {
                var buf = message as IOBuffer;
                if (buf != null)
                {
                    // manipulate bytes
                    ManipulateIoBuffer(session, buf);
                    var buffer = InsertBytesToNewIoBuffer(session, buf);
                    if (buffer != null)
                    {
                        message = buffer;
                    }
                }
            }

            base.MessageReceived(nextFilter, session, message);
        }

        private IOBuffer InsertBytesToNewIoBuffer(IOSession session, IOBuffer buffer)
        {
            if (InsertByteProbability > _rng.Next(1000))
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(buffer.GetHexDump());
                }

                // where to insert bytes ?
                var pos = _rng.Next(buffer.Remaining) - 1;

                // how many byte to insert ?
                var count = _rng.Next(MaxInsertByte - 1) + 1;

                var newBuff = IOBuffer.Allocate(buffer.Remaining + count);
                for (var i = 0; i < pos; i++)
                {
                    newBuff.Put(buffer.Get());
                }
                for (var i = 0; i < count; i++)
                {
                    newBuff.Put((byte) (_rng.Next(256)));
                }
                while (buffer.Remaining > 0)
                {
                    newBuff.Put(buffer.Get());
                }
                newBuff.Flip();

                if (Log.IsInfoEnabled)
                {
                    Log.Info("Inserted " + count + " bytes.");
                    Log.Info(newBuff.GetHexDump());
                }
                return newBuff;
            }
            return null;
        }

        private void ManipulateIoBuffer(IOSession session, IOBuffer buffer)
        {
            if ((buffer.Remaining > 0) && (RemoveByteProbability > _rng.Next(1000)))
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(buffer.GetHexDump());
                }

                // where to remove bytes ?
                var pos = _rng.Next(buffer.Remaining);
                // how many byte to remove ?
                var count = _rng.Next(buffer.Remaining - pos) + 1;
                if (count == buffer.Remaining)
                {
                    count = buffer.Remaining - 1;
                }

                var newBuff = IOBuffer.Allocate(buffer.Remaining - count);
                for (var i = 0; i < pos; i++)
                {
                    newBuff.Put(buffer.Get());
                }

                buffer.Skip(count); // hole
                while (newBuff.Remaining > 0)
                {
                    newBuff.Put(buffer.Get());
                }
                newBuff.Flip();
                // copy the new buffer in the old one
                buffer.Rewind();
                buffer.Put(newBuff);
                buffer.Flip();

                if (Log.IsInfoEnabled)
                {
                    Log.Info("Removed " + count + " bytes at position " + pos + ".");
                    Log.Info(buffer.GetHexDump());
                }
            }
            if ((buffer.Remaining > 0) && (ChangeByteProbability > _rng.Next(1000)))
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(buffer.GetHexDump());
                }

                // how many byte to change ?
                var count = _rng.Next(buffer.Remaining - 1) + 1;

                var values = new byte[count];
                _rng.NextBytes(values);
                for (var i = 0; i < values.Length; i++)
                {
                    var pos = _rng.Next(buffer.Remaining);
                    buffer.Put(pos, values[i]);
                }

                if (Log.IsInfoEnabled)
                {
                    Log.Info("Modified " + count + " bytes.");
                    Log.Info(buffer.GetHexDump());
                }
            }
        }

        public int RemoveByteProbability { get; set; }

        public int InsertByteProbability { get; set; }

        public int ChangeByteProbability { get; set; }

        public int RemovePduProbability { get; set; }

        public int DuplicatePduProbability { get; set; }

        public int ResendPduLasterProbability { get; set; }

        public int MaxInsertByte { get; set; } = 10;

        public bool ManipulateWrites { get; set; }

        public bool ManipulateReads { get; set; }
    }
}
