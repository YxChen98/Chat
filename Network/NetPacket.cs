using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Network
{
    public class NetPacket
    {
        //define int32=4bytes, header=4bytes
        //header contains data length
        //because we are not sure how long the data will be when receiving
        public const int INT32_LEN = 4;
        public const int headerLength = 4;

        public const int max_body_length = 512;
        public int bodyLength { get; set; }

        public int length
        {
            get { return headerLength + bodyLength; }
        }

        public byte[] bytes { get; set; }

        //declare the socket which sends this packet
        public Socket socket;

        //data length read from internet
        //wait until this is the same as total length, to confirm completion of receive
        public int readLength { get; set; }

        //constructor
        public NetPacket()
        {
            readLength = 0;
            bodyLength = 0;
            bytes = new byte[headerLength + max_body_length];
        }

        public void Reset()
        {
            readLength = 0;
            bodyLength = 0;
        }

        //create binary formatter to serialize data into stream
        public byte[] Serialize<T>(T t)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(stream, t);

                    stream.Seek(0, SeekOrigin.Begin);
                    return stream.ToArray();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
        }

        //deserialize from stream
        public T Deserialize<T>(byte[] bs)
        {
            using (MemoryStream stream = new MemoryStream(bs))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    T t = (T)bf.Deserialize(stream);
                    return t;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Deserialize: " + e.Message);
                    return default(T);
                }
            }
        }

        /////////////////////////////////Write serialized data into stream:

        //if int, call this to write int into stream
        public void WriteInt(int number)
        {
            if (bodyLength + INT32_LEN > max_body_length)
                return;

            byte[] bs = System.BitConverter.GetBytes(number);
            bs.CopyTo(bytes, headerLength + bodyLength);
            bodyLength += INT32_LEN;

        }

        //if string, call this to write stirng into stream
        public void WriteString(string str)
        {
            int len = System.Text.Encoding.UTF8.GetByteCount(str);
            this.WriteInt(len);

            if (bodyLength + len > max_body_length)
                return;

            System.Text.Encoding.UTF8.GetBytes(str, 0, str.Length, bytes, headerLength + bodyLength);
            bodyLength += len;
        }

        //write byte array
        public void WriteStream(byte[] bs)
        {
            WriteInt(bs.Length);

            if (bodyLength + bs.Length > max_body_length)
                return;

            bs.CopyTo(bytes, headerLength + bodyLength);
            bodyLength += bs.Length;
        }

        //write any object into stream
        public void WriteObject<T>(T t)
        {
            byte[] bs = Serialize<T>(t);
            WriteStream(bs);
        }

        public void BeginWrite(string msg)
        {
            bodyLength = 0;
            WriteString(msg);
        }

        //create message header, header is the message length
        public void EncodeHeader()
        {
            byte[] bs = System.BitConverter.GetBytes(bodyLength);
            bs.CopyTo(bytes, 0);
        }

        ////////////////////////////Read data from stream:

        //Read int:
        public void ReadInt(out int number)
        {
            number = 0;
            if (bodyLength + INT32_LEN > max_body_length)
                return;
            number = System.BitConverter.ToInt32(bytes, headerLength + bodyLength);
            bodyLength += INT32_LEN;
        }

        //Read string:
        public void ReadString(out string str)
        {
            str = "";
            int len = 0;
            ReadInt(out len);

            if (bodyLength + len > max_body_length)
                return;

            str = Encoding.UTF8.GetString(bytes, headerLength + bodyLength, (int)len);
            bodyLength += len;
        }

        //Read byte array
        public byte[] ReadStream()
        {
            int size = 0;
            ReadInt(out size);
            if (bodyLength + INT32_LEN > max_body_length)
                return null;

            byte[] bs = new byte[size];
            for (int i = 0; i < size; i++)
            {
                bs[i] = bytes[headerLength + bodyLength + i];
            }

            bodyLength += size;
            return bs;
        }

        //Read any object
        public T ReadObject<T>()
        {
            byte[] bs = ReadStream();
            if (bs == null)
                return default(T);

            return Deserialize<T>(bs);
        }

        public void BeginRead(out string msg)
        {
            bodyLength = 0;
            ReadString(out msg);
        }

        //decode body length from header
        public void DecodeHeader()
        {
            bodyLength = System.BitConverter.ToInt32(bytes, 0);
        }
    }
}