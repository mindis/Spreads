// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Buffers;

namespace Spreads.Serialization
{
    internal class ByteArrayBinaryConverter : IBinaryConverter<byte[]>
    {
        public bool IsFixedSize => false;
        public int Size => 0;

        public int SizeOf(byte[] value, out MemoryStream temporaryStream, CompressionMethod compression = CompressionMethod.DefaultOrNone)
        {
            if (compression == CompressionMethod.DefaultOrNone)
            {
                temporaryStream = null;
                return value.Length + 8;
            }
            else
            {
                return CompressedArrayBinaryConverter<byte>.Instance.SizeOf(value, 0, value.Length, out temporaryStream, compression);
            }
        }

        public unsafe int Write(byte[] value, ref Buffer<byte> destination, uint offset = 0, MemoryStream temporaryStream = null, CompressionMethod compression = CompressionMethod.DefaultOrNone)
        {
            if (compression == CompressionMethod.DefaultOrNone)
            {
                if (temporaryStream != null)
                    throw new NotSupportedException("ByteArrayBinaryConverter does not work with temp streams.");
                var totalSize = value.Length + 8;
                if (destination.Length < offset + totalSize)
                {
                    return (int)BinaryConverterErrorCode.NotEnoughCapacity;
                }

                var handle = destination.Pin();
                var ptr = (IntPtr)handle.PinnedPointer + (int)offset;
                // size
                Marshal.WriteInt32(ptr, totalSize);
                // version
                Marshal.WriteByte(ptr + 4, Version);
                // payload
                Marshal.Copy(value, 0, ptr + 8, value.Length);
                handle.Free();
                return totalSize;
            }
            else
            {
                return CompressedArrayBinaryConverter<byte>.Instance.Write(value, 0, value.Length, ref destination,
                    offset, temporaryStream, compression);
            }
        }

        public int Read(IntPtr ptr, out byte[] value)
        {
            var totalSize = Marshal.ReadInt32(ptr);
            var versionFlags = Marshal.ReadByte(ptr + 4);
            var version = (byte)(versionFlags >> 4);
            var isCompressed = (versionFlags & 0b0000_0001) != 0;
            if (version != 0) throw new NotSupportedException("ByteArrayBinaryConverter work only with version 0");

            if (!isCompressed)
            {
                var bytes = new byte[totalSize - 8];
                Marshal.Copy(ptr + 8, bytes, 0, bytes.Length);
                value = bytes;
                return totalSize;
            }
            else
            {
                var len = CompressedArrayBinaryConverter<byte>.Instance.Read(ptr, out var tmp, out int count, true);
                value = tmp;
                return len;
            }
        }

        public byte Version => 0;
    }
}