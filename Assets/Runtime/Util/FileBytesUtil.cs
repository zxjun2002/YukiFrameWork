using System.IO;
using MemoryPack;

public class FileBytesUtil
{
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