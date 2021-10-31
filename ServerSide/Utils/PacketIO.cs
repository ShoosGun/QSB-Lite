using System;
using System.IO;

namespace ServerSide.Utils
{
    public class PacketWriter : BinaryWriter
    {
        private MemoryStream _memStream;

        public PacketWriter() : base()
        {
            _memStream = new MemoryStream();
            OutStream = _memStream;
        }
        public byte[] GetBytes()
        {
            Close();
            byte[] data = _memStream.ToArray();
            return data;
        }
        public void WriteAsArray(byte[] array)
        {
            Write(array.Length);
            Write(array);
        }
        public void Write(byte[][] byteMatrix)
        {
            int lenght = byteMatrix.Length;
            Write(lenght);
            for (int i = 0; i < lenght; i++)
                WriteAsArray(byteMatrix[i]);
        }
        public void Write(int[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        public void Write(int[][] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }
        /// <summary>
        /// Use DateTime.UtcNow to avoid problems
        /// </summary>
        /// <param name="dateTime"></param>
        public void Write(DateTime dateTime)
        {
            Write(dateTime.ToBinary());
        }
    }

    public class PacketReader : BinaryReader
    {
        public PacketReader(byte[] data) : base(new MemoryStream(data))
        {
        }

        public byte[] ReadByteArray()
        {
            int arrayLenght = ReadInt32();
            return ReadBytes(arrayLenght);
        }
        public byte[][] ReadByteMatrix()
        {
            int lenght = ReadInt32();
            byte[][] byteMatrix = new byte[lenght][];
            for (int i = 0; i < lenght; i++)
                byteMatrix[i] = ReadByteArray();

            return byteMatrix;
        }
        public int[] ReadInt32Array()
        {
            int lenght = ReadInt32();
            int[] intArray = new int[lenght];
            for (int i = 0; i < lenght; i++)
                intArray[i] = ReadInt32();

            return intArray;
        }
        public int[][] ReadInt32ArrayArray()
        {

            int lenght = ReadInt32();
            int[][] intArrayArray = new int[lenght][];
            for (int i = 0; i < lenght; i++)
                intArrayArray[i] = ReadInt32Array();

            return intArrayArray;
        }
        /// <summary>
        /// Read as if the DateTime was a DateTime.UtcNow to avoid problems
        /// </summary>
        /// <returns></returns>
        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadInt64());
        }
    }
}