using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using System.Net;

//Header
//1. enum MSG type --> byte
//2. type ID --> uint
public class Packet 
{
    public bool esential;
    public bool externalServer = false;
    public DateTime timestamp;
    public IPEndPoint sender = null;
    public IPEndPoint remote = null;
    public uint pakID = 10;
    //public bool disposed = false;
    private List<byte> buffer;
    private byte[] readableBuffer;
    private int readPos;
    public int size;

    //Creates a new packet without an ID
    public Packet(bool es = false)
    {
        buffer = new List<byte>();
        readPos = 0;
        timestamp = DateTime.Now;
        pakID = 0;
        esential = es;
    }

    //Creates a new packet with a given ID
    public Packet(int _id,bool es = false)
    {
        buffer = new List<byte>(); 
        readPos = 0;
        timestamp = DateTime.Now;
        pakID = 0;
        Write(_id);
        esential = es;
    }

    //Creates a packet with starting data
    public Packet(byte[] _data, bool es = false)
    {
        buffer = new List<byte>(); 
        readPos = 0;
        timestamp = DateTime.Now;
        pakID = 0;
        SetBytes(_data);
        esential = es;
    }

    public Packet(byte[] _data,IPEndPoint remote,uint id = 0, bool es = false)
    {
        timestamp = DateTime.Now;
        sender = remote;
        pakID = id;
        buffer = new List<byte>();
        readPos = 0;
        SetBytes(_data);
        esential = es;
    }

    public uint ReadID()
    {
        if(readableBuffer.Length > 0) pakID = ReadUInt();
        return pakID;
    }

    public void SetBytes(byte[] _data)
    {
        Write(_data);
        readableBuffer = buffer.ToArray();
    }

    //Inserts the length of the packet's content at the start of the buffer
    public void WriteLength()
    {
        buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));
    }

    //Adds the header
    public void AddHeader(uint id,byte msg)
    {
        buffer.InsertRange(0, BitConverter.GetBytes(msg));
        buffer.InsertRange(0, BitConverter.GetBytes(id));      
    }

    //Adds the header
    public void RemoveHeader()
    {
        buffer.RemoveRange(0,5);
        readableBuffer = buffer.ToArray();
    }

    //Inserts the given int at the start of the buffer
    public void InsertInt(int _value)
    {
        buffer.InsertRange(0, BitConverter.GetBytes(_value));
    }

    //Inserts the given byte at the start of the buffer
    public void InsertByte(byte _value)
    {
        buffer.InsertRange(0, BitConverter.GetBytes(_value));
    }

    //Gets the packet's content as array
    public byte[] ToArray()
    {
        readableBuffer = buffer.ToArray();
        return readableBuffer;
    }

    public string ToStringBitArray()
    {
        readableBuffer = buffer.ToArray();
        return BitConverter.ToString(readableBuffer);
    }

    //Gets the length of the content
    public int Length()
    {
        return buffer.Count;
    }

    //Gets the length of the unread data
    public int UnreadLength()
    {
        return Length() - readPos;
    }

    //Resets the packet and clear all data
    public void Reset()
    {
        buffer.Clear();
        readableBuffer = null;
        readPos = 0;       
    }

    public bool HasBufer()
    {
        if (readableBuffer != null && readableBuffer.Length > 0) return true;
        else return false;
    }

    public bool AllRead()
    {
        if (readPos >= size) return true;
        else return false;
    }

    #region Write
    //Adds a byte
    public void Write(byte _value)
    {
        buffer.Add(_value);
    }

    //Adds an array of bytes
    public void Write(byte[] _value)
    {
        buffer.AddRange(_value);
    }

    //Adds a short
    public void Write(short _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //Adds a ushort
    public void Write(ushort _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //Adds an int
    public void Write(int _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //Adds an uint
    public void Write(uint _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //Adds a long
    public void Write(long _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //Adds a ulong
    public void Write(ulong _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //Adds a float
    public void Write(float _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //Adds a bool
    public void Write(bool _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //Adds a string
    public void Write(string _value)
    {
        Write(_value.Length);
        buffer.AddRange(Encoding.ASCII.GetBytes(_value));
    }

    public void Write(Color _value)
    {
        Write(_value.r);
        Write(_value.g);
        Write(_value.b);
    }

    public void Write(Vector3 _value)
    {
        Write(_value.x);
        Write(_value.y);
        Write(_value.z);
    }

    public void Write(Vector2 _value)
    {
        Write(_value.x);
        Write(_value.y);
    }

    public void Write(Quaternion _value)
    {
        Write(_value.x);
        Write(_value.y);
        Write(_value.z);
        Write(_value.w);
    }

    public void Write(BitArray _value)
    {
        byte[] array = new byte[_value.Length/8];
        _value.CopyTo(array,0);
        buffer.AddRange(array);
    }
    #endregion

    #region Read Data

    //Reads a byte
    public byte ReadByte(bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            byte _value = readableBuffer[readPos]; 
            if(movePos) readPos += 1;            
            return _value; 
        }
        else
        {
            throw new Exception("Could not read value of type 'byte'!");
        }
    }

    //Reads an array of bytes
    public byte[] ReadBytes(int _length, bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            byte[] _value = buffer.GetRange(readPos, _length).ToArray();
            if (movePos) readPos += _length;           
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'byte[]'!");
        }
    }

    //Reads a short
    public short ReadShort(bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            short _value = BitConverter.ToInt16(readableBuffer, readPos);
            if (movePos) readPos += 2;            
            return _value; 
        }
        else
        {
            throw new Exception("Could not read value of type 'short'!");
        }
    }

    //Reads a ushort
    public ushort ReadUShort(bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            ushort _value = BitConverter.ToUInt16(readableBuffer, readPos);
            if (movePos) readPos += 2;
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'ushort'!");
        }
    }

    public BitArray ReadBits(int bitsNum,bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            byte[] array = new byte[bitsNum/8];
            for (int i = 0;i<array.Length;i++)
            {
                array[i] = readableBuffer[readPos + i];
            }
            if (movePos) readPos += bitsNum/8;
            return new BitArray(array);
        }
        else
        {
            throw new Exception("Could not read value of type 'short'!");
        }
    }

    //Reads an int
    public int ReadInt(bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            int _value = BitConverter.ToInt32(readableBuffer, readPos);
            if (movePos) readPos += 4;           
            return _value; 
        }
        else
        {
            throw new Exception("Could not read value of type 'int'!");
        }
    }

    //Reads an uint
    public uint ReadUInt(bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            uint _value = BitConverter.ToUInt32(readableBuffer, readPos);
            if (movePos) readPos += 4;
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'uint'!");
        }
    }

    //Reads a long
    public long ReadLong(bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            long _value = BitConverter.ToInt64(readableBuffer, readPos);
            if (movePos) readPos += 8;           
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'long'!");
        }
    }

    //Reads a ulong
    public ulong ReadULong(bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            ulong _value = BitConverter.ToUInt64(readableBuffer, readPos);
            if (movePos) readPos += 8;
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'ulong'!");
        }
    }

    //Reads a float
    public float ReadFloat(bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            float _value = BitConverter.ToSingle(readableBuffer, readPos);
            if (movePos) readPos += 4;             
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'float'!");
        }
    }

    //Reads a bool
    public bool ReadBool(bool movePos = true)
    {
        if (buffer.Count > readPos)
        {
            bool _value = BitConverter.ToBoolean(readableBuffer, readPos);
            if (movePos) readPos += 1;
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'bool'!");
        }
    }

    //Reads a string
    public string ReadString(bool movePos = true)
    {
        try
        {
            int _length = ReadInt();
            string _value = Encoding.ASCII.GetString(readableBuffer, readPos, _length);
            if (movePos && _value.Length > 0) readPos += _length;

            return _value;
        }
        catch
        {
            throw new Exception("Could not read value of type 'string'!");
        }
    }
    public Color ReadColor(bool movePos = true)
    {
        try
        {
            return new Color(ReadFloat(movePos), ReadFloat(movePos), ReadFloat(movePos));
        }
        catch
        {
            throw new Exception("Could not read value of type 'color'!");
        }
    }
    public Vector2 ReadVector2(bool movePos = true)
    {
        try
        {
            return new Vector2(ReadFloat(movePos), ReadFloat(movePos));
        }
        catch
        {
            throw new Exception("Could not read value of type 'vector2'!");
        }
    }
    public Vector3 ReadVector3(bool movePos = true)
    {
        try
        {
            return new Vector3(ReadFloat(movePos), ReadFloat(movePos), ReadFloat(movePos));
        }
        catch
        {
            throw new Exception("Could not read value of type 'vector3'!");
        }
    }

    public Quaternion ReadQuaternion(bool movePos = true)
    {
        try
        {
            return new Quaternion(ReadFloat(movePos), ReadFloat(movePos), ReadFloat(movePos), ReadFloat(movePos));
        }
        catch
        {
            throw new Exception("Could not read value of type 'quaternion'!");
        }
    }
    #endregion
}