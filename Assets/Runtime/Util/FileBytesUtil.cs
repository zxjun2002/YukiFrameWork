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

    /// <summary>
    /// 从字节文件中加载并反序列化为对象
    /// </summary>
    public static T LoadConfFromBinary<T>(string filePath)
    {
        // 读取文件内容到字节数组
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found at path: {filePath}");
        }

        byte[] byteData = File.ReadAllBytes(filePath);

        // 反序列化字节数组到对象
        T conf = MemoryPackSerializer.Deserialize<T>(byteData);
        return conf;
    }
}