using System.IO;
using MemoryPack;

public class FileBytesUtil
{
    // 将普通类序列化并保存为字节文件
    public static void SaveConfDataToByteFile<T>(T conf,string filePath)
    {
        //序列化对象到字节数组
        byte[] byteData = MemoryPackSerializer.Serialize(conf);
        File.WriteAllBytes(filePath, byteData);
    }

    public static T LoadConfFromBinary<T>(byte[] byteData)
    {
        T conf = MemoryPackSerializer.Deserialize<T>(byteData);
        return conf;
    }
}