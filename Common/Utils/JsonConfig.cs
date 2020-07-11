namespace Common
{
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    public static class JsonConfig
    {
        public static async Task Write<T>(T options, string fileName)
            where T : class
        {
            using (FileStream fs = File.OpenWrite(fileName))
            {
                await JsonSerializer.SerializeAsync(fs, options, options.GetType());
            }
        }

        public static async Task<T> Read<T>(string fileName)
            where T : class, new()
        {
            try
            {
                using (FileStream fs = File.OpenRead(fileName))
                {
                    return await JsonSerializer.DeserializeAsync<T>(fs);
                }
            }
            catch
            {
                return new T();
            }
        }
    }
}
