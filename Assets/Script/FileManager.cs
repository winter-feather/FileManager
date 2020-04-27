using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
public class FileManager : MonoBehaviour
{
    [HideInInspector]
    public byte[] key;
    List<TestEntity> testEntityList;

    private void Start()
    {
        key = new byte[] { 200, 230, 200, 200, 202, 19, 84, 05, 19, 68, 17};
        #region Test
        string p1 = Application.streamingAssetsPath + @"\Table01.txt";
        string p2 = Application.streamingAssetsPath + @"\Table01.dat";

        LoadTxtSaveToData<TestEntity>(p1, p2);

        testEntityList = LoadData2EntityList<TestEntity>(p2);

        for (int i = 0; i < testEntityList.Count; i++)
        {
            Debug.Log(testEntityList[i].ToString());
        }
        #endregion

    }

    public List<T> LoadData2EntityList<T>(string path) where T : BaseEntity
    {
        byte[] buffer = LoadData(path);
        return ConvertByte2EntityList<T>(buffer);
    }
    public void LoadTxtSaveToData<T>(string loadPath, string savePath) where T : BaseEntity
    {
        string s1 = LoadTextFile(loadPath);//Application.streamingAssetsPath + @"\Table01.txt"
        List<T> dataList = ConvertString2EntityList<T>(s1);
        using (CustomStream cs = new CustomStream())
        {
            cs.WriteInt(dataList.Count);
            for (int i = 0; i < dataList.Count; i++)
            {
                byte[] beBuffer = dataList[i].Save();
                for (int j = 0; j < beBuffer.Length; j++)
                {
                    cs.WriteByte(beBuffer[j]);
                }
            }
            byte[] buffer = cs.ToArray();
            SaveToData(buffer, savePath);
        }
    }

    List<T> ConvertString2EntityList<T>(string data) where T : BaseEntity
    {
        string[] datas = data.Split('\n');
        List<T> dataList = new List<T>();
        for (int i = 1; i < datas.Length - 1; i++)
        {
            dataList.Add(BaseEntity.Instantiation(typeof(T), datas[i]) as T);
        }
        return dataList;
    }
    List<T> ConvertByte2EntityList<T>(byte[] buffer) where T : BaseEntity
    {
        List<T> dataList = new List<T>();
        using (CustomStream cs = new CustomStream(buffer))
        {
            int count = cs.ReadInt();
            for (int i = 0; i < count; i++)
            {
                int size = cs.ReadInt();
                byte[] data = new byte[size];
                cs.Read(data, 0, size);
                dataList.Add(BaseEntity.Instantiation(typeof(T), data) as T);
            }
            return dataList;
        }
    }
    byte[] LoadData(string path)
    {
        using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
        {
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            Decryption(key, buffer);
            return buffer;
        }
    }
    string LoadTextFile(string path)
    {
        using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
        {
            StreamReader sr = new StreamReader(fs);
            string s = sr.ReadToEnd();
            fs.Dispose();
            return s;
        }
    }
    void SaveToData(string data, string path)
    {
        byte[] buffer = System.Text.Encoding.Default.GetBytes(data);
        SaveToData(buffer, path);
    }
    void SaveToData(byte[] buffer, string path)
    {
        using (FileStream fs = new FileStream(path, FileMode.Create))
        {
            Encryption(key, buffer);
            fs.Write(buffer, 0, buffer.Length);
        }
    }
    void Decryption(byte[] key, byte[] data)
    {
        Encryption(key, data);
    }
    void Encryption(byte[] key, byte[] data)
    {
        int c = 0;
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(data[i] ^ key[c++]);
            if (c >= key.Length) c -= key.Length;
        }
    }
}
public class TestEntity : BaseEntity
{
    public int id;
    public string name;
    public int p1;
    public float p2;
    public bool p3;
    public TestEntity() { }
    public TestEntity(string s)
    {
        string[] ss = s.Split('\t');
        id = int.Parse(ss[0]);
        name = ss[1];
        p1 = int.Parse(ss[2]);
        p2 = float.Parse(ss[3]);
        p3 = int.Parse(ss[4]) == 0 ? false : true;
    }
    public TestEntity(byte[] data)
    {
        Load(data);
    }

    public override void Load(byte[] data)
    {
        using (CustomStream ms = new CustomStream(data))
        {
            id = ms.ReadInt();
            name = ms.ReadString();
            p1 = ms.ReadInt();
            p2 = ms.ReadFloat();
            p3 = ms.ReadBoolean();
        }
    }

    public override byte[] Save()
    {
        using (CustomStream ms = new CustomStream())
        {
            ms.WriteInt(13 + 4 + name.Length);
            ms.WriteInt(id);
            ms.WriteString(name);
            ms.WriteInt(p1);
            ms.WriteFloat(p2);
            ms.WriteBoolean(p3);
            return ms.ToArray();
        }
    }

    public override string ToString()
    {
        return string.Format("id:{0},name:{1},p1:{2},p2:{3},p3:{4}", id, name, p1, p2, p3);
    }
}
public class BaseEntity : IAccessible
{
    public static BaseEntity Instantiation(Type type, byte[] buffer)
    {
        if (type == typeof(TestEntity))
        {
            return new TestEntity(buffer);
        }
        return new BaseEntity(buffer);
    }
    public static BaseEntity Instantiation(Type type, string data)
    {
        if (type == typeof(TestEntity))
        {
            return new TestEntity(data);
        }
        return new BaseEntity(data);
    }
    public BaseEntity() { }
    public BaseEntity(string s) { }
    public BaseEntity(byte[] data) { Load(data); }
    public virtual void Load(byte[] data) { }
    public virtual byte[] Save() { return null; }
}
public class CustomStream : MemoryStream
{
    public CustomStream() : base() { }
    public CustomStream(byte[] buffer) : base(buffer) { }

    public void WriteInt(int data)
    {
        byte[] buffer = BitConverter.GetBytes(data);
        Write(buffer, 0, buffer.Length);
    }
    public int ReadInt()
    {
        byte[] buffer = new byte[4];
        Read(buffer, 0, 4);
        return BitConverter.ToInt32(buffer, 0);
    }
    public void WriteFloat(float data)
    {
        byte[] buffer = BitConverter.GetBytes(data);
        Write(buffer, 0, buffer.Length);
    }
    public float ReadFloat()
    {
        byte[] buffer = new byte[4];
        Read(buffer, 0, 4);
        return BitConverter.ToSingle(buffer, 0);
    }
    public void WriteChar(char data)
    {
        byte[] buffer = BitConverter.GetBytes(data);
        Write(buffer, 0, buffer.Length);
    }
    public char ReadChar()
    {
        byte[] buffer = new byte[2];
        Read(buffer, 0, 2);
        return BitConverter.ToChar(buffer, 0);
    }
    public void WriteBoolean(bool data)
    {
        byte[] buffer = BitConverter.GetBytes(data);
        Write(buffer, 0, buffer.Length);
    }
    public bool ReadBoolean()
    {
        byte[] buffer = new byte[1];
        Read(buffer, 0, 1);
        return BitConverter.ToBoolean(buffer, 0);
    }
    public void WriteString(string data)
    {
        byte[] buffer = System.Text.Encoding.Default.GetBytes(data);
        WriteInt(buffer.Length);
        Write(buffer, 0, buffer.Length);
    }
    public string ReadString()
    {
        int length = ReadInt();
        byte[] buffer = new byte[length];
        Read(buffer, 0, length);
        return System.Text.Encoding.Default.GetString(buffer);
    }
}
public interface IAccessible
{
    byte[] Save();
    void Load(byte[] data);
}
