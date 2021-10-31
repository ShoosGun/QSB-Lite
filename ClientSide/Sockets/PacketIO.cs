using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace ClientSide.Sockets
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
        public void Write(Vector2 vector)
        {
            Write(vector.x);
            Write(vector.y);
        }
        public void Write(Vector3 vector)
        {
            Write(vector.x);
            Write(vector.y);
            Write(vector.z);
        }
        public void Write(Vector4 vector)
        {
            Write(vector.x);
            Write(vector.y);
            Write(vector.z);
            Write(vector.x);
        }
        public void Write(Quaternion quaternion)
        {
            Write(quaternion.x);
            Write(quaternion.y);
            Write(quaternion.z);
            Write(quaternion.x);
        }
        /// <summary>
        /// Use DateTime.UtcNow to avoid problems
        /// </summary>
        /// <param name="dateTime"></param>
        public void Write(DateTime dateTime)
        {
            Write(dateTime.ToBinary());
        }

        public void WriteAsObjectArray(object[] array)
        {
            Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                WriteAsObject(array[i]);
        }
        public void WriteAsObject(object Object) //TODO for my sanity, make this function better
        {
            Type objectType = Object.GetType();

            if (objectType == typeof(byte))
            {
                Write((byte)SentObjectType.BYTE);
                Write((byte)Object);
            }
            else if (objectType == typeof(short))
            {
                Write((byte)SentObjectType.SHORT);
                Write((short)Object);
            }
            else if (objectType == typeof(int))
            {
                Write((byte)SentObjectType.INT);
                Write((int)Object);
            }
            else if (objectType == typeof(long))
            {
                Write((byte)SentObjectType.LONG);
                Write((long)Object);
            }

            else if (objectType == typeof(float))
            {
                Write((byte)SentObjectType.FLOAT);
                Write((float)Object);
            }
            else if (objectType == typeof(double))
            {
                Write((byte)SentObjectType.DOUBLE);
                Write((double)Object);
            }

            else if (objectType == typeof(Vector2))
            {
                Write((byte)SentObjectType.VECTOR2);
                Write((Vector2)Object);
            }
            else if (objectType == typeof(Vector3))
            {
                Write((byte)SentObjectType.VECTOR3);
                Write((Vector3)Object);
            }
            else if (objectType == typeof(Vector4))
            {
                Write((byte)SentObjectType.VECTOR4);
                Write((Vector4)Object);
            }
            else if (objectType == typeof(Quaternion))
            {
                Write((byte)SentObjectType.QUATERNION);
                Write((Quaternion)Object);
            }

            else if (objectType == typeof(string))
            {
                Write((byte)SentObjectType.STRING);
                Write((string)Object);
            }

            else if (objectType == typeof(byte[]))
            {
                Write((byte)(SentObjectType.BYTE | SentObjectType.ARRAY));
                WriteAsArray((byte[])Object);
            }
            else if (objectType == typeof(int[]))
            {
                Write((byte)(SentObjectType.INT | SentObjectType.ARRAY));
                Write((int[])Object);
            }
            else
                throw new ArgumentException(string.Format("The type {0} isn't currently supported by this function", objectType));
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
        public Vector2 ReadVector2()
        {
            return new Vector2(ReadSingle(), ReadSingle());
        }
        public Vector3 ReadVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }
        public Vector4 ReadVector4()
        {
            return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }
        public Quaternion ReadQuaternion()
        {
            return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }
        /// <summary>
        /// Read as if the DateTime was a DateTime.UtcNow to avoid problems
        /// </summary>
        /// <returns></returns>
        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadInt64());
        }

        public object[] ReadObjectArray()
        {
            int lenght = ReadInt32();
            object[] objArray = new object[lenght];
            for (int i = 0; i < lenght; i++)
                objArray[i] = ReadObject();

            return objArray;
        }
        public object ReadObject()
        {
            byte objectType = ReadByte();
            object sentObject;
            switch (objectType)
            {
                case (byte)SentObjectType.BYTE:
                    sentObject = ReadByte();
                    break;
                case (byte)SentObjectType.SHORT:
                    sentObject = ReadInt16();
                    break;
                case (byte)SentObjectType.INT:
                    sentObject = ReadInt32();
                    break;
                case (byte)SentObjectType.LONG:
                    sentObject = ReadInt64();
                    break;

                case (byte)SentObjectType.FLOAT:
                    sentObject = ReadSingle();
                    break;
                case (byte)SentObjectType.DOUBLE:
                    sentObject = ReadDouble();
                    break;

                case (byte)SentObjectType.VECTOR2:
                    sentObject = ReadVector2();
                    break;
                case (byte)SentObjectType.VECTOR3:
                    sentObject = ReadVector3();
                    break;
                case (byte)SentObjectType.VECTOR4:
                    sentObject = ReadVector4();
                    break;
                case (byte)SentObjectType.QUATERNION:
                    sentObject = ReadQuaternion();
                    break;

                case (byte)SentObjectType.STRING:
                    sentObject = ReadString();
                    break;

                case (byte)(SentObjectType.BYTE | SentObjectType.ARRAY):
                    sentObject = ReadByteArray();
                    break;
                case (byte)(SentObjectType.INT | SentObjectType.ARRAY):
                    sentObject = ReadInt32Array();
                    break;

                default:
                    sentObject = new object();
                    break;
            }

            return sentObject;
        }
    }
    public enum SentObjectType : byte
    {
        BYTE,
        SHORT,
        INT,
        LONG,

        FLOAT,
        DOUBLE,

        VECTOR2,
        VECTOR3,
        VECTOR4,
        QUATERNION,

        STRING,

        ARRAY = 128 //Ultimo byte
    }
}