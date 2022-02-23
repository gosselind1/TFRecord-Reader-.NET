using System;
using System.IO;
using Force.Crc32;

namespace TFRecord_Reader
{
    // Adapted from java implementation at:
    // https://github.com/tensorflow/ecosystem/blob/master/hadoop/src/main/java/org/tensorflow/hadoop/util/TFRecordReader.java
    // With additional insight/code from:
    // https://github.com/kevinskii/TFRecord.NET

    public class TFRecordReader : ITFRecordReader
    {
        // CRC32 Mask in tensorflow crc module
        private const uint MASK_DELTA = 0xa282ead8;

        // fields from java implementation
        private readonly Stream input;
        private readonly Boolean crcCheck;

        public TFRecordReader(Stream input, Boolean crcCheck)
        {
            this.input = input;
            this.crcCheck = crcCheck;
        }

        public byte[] Read()
        {
            /**
            * TFRecord format:
            * uint64 length
            * uint32 masked_crc32_of_length
            * byte   data[length]
            * uint32 masked_crc32_of_data
            */
            byte[] lenBytes = new byte[8];
            try
            {
                // catch EOF. Other issues indicate file corruption
                ReadFully(input, lenBytes);
            } catch (EndOfStreamException eof)
            {
                return null;
            }

            long len = FromInt64LittleEndian(lenBytes);
            // validate crc32
            if (!crcCheck)
            {
                input.Position += 4;  // todo: validate that this works
            } else
            {
                byte[] lenCrc32Bytes = new byte[4];
                ReadFully(input, lenCrc32Bytes);
                int lenCrc32 = FromInt32LittleEndian(lenCrc32Bytes);
                if (lenCrc32 != MaskedCRC32c(lenBytes))
                {
                    throw new IOException("Lenght header crc32 checking failed: " + lenCrc32 + " != " +
                        MaskedCRC32c(lenBytes) + ", length = " + len);
                }
            }

            if (len > Int32.MaxValue)
            {
                throw new IOException("Record size exceeds max value of int32: " + len);
            }

            byte[] data = new byte[len];
            ReadFully(input, data);

            // Verify data crc32
            if (!crcCheck)
            {
                input.Position += 4;
            } else
            {
                byte[] dataCrc32Bytes = new byte[4];
                ReadFully(input, dataCrc32Bytes);
                int dataCrc32 = FromInt32LittleEndian(dataCrc32Bytes);
                if (dataCrc32 != MaskedCRC32c(data))
                {
                    throw new IOException("Data crc32 checking failed: " + dataCrc32 + " != " +
                        MaskedCRC32c(data));
                }
            }
            return data;
        }

        private long FromInt64LittleEndian(byte[] data)
        {
            // Assume length of data is 8. Original code contains assertion
            // convert little endian to cpu endian. TFRecords are stored in little
            if (!System.BitConverter.IsLittleEndian)
                Array.Reverse(data);

            long value = System.BitConverter.ToInt64(data, 0);

            return value;
        }

        private int FromInt32LittleEndian(byte[] data)
        {
            // Assume length of data is 4.
            // Convert to cpu endian
            if (!System.BitConverter.IsLittleEndian)
                Array.Reverse(data);

            int value = System.BitConverter.ToInt32(data, 0);

            return value;
        }

        private void ReadFully(Stream input, byte[]  buffer)
        {
            int nbytes;
            for (int nread = 0; nread < buffer.Length; nread += nbytes)
            {
                nbytes = input.Read(buffer, nread, buffer.Length - nread);
                if (nbytes < 0)
                    throw new EndOfStreamException("End of file reached before reading fully.");
            }
        }

        private int MaskedCRC32c(byte[] data)
        {
            uint crc;
            crc = Force.Crc32.Crc32CAlgorithm.Compute(data);
            crc = ((crc >> 15) | (crc << 17)) + MASK_DELTA;

            return (int)crc;
        }
    }
}
