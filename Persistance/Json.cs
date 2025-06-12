using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TaskbarTray.Persistance;

public static class Json
{
    public static async Task<T> ToObjectAsync<T>(string value)
    {
        return await Task.Run(() =>
        {
            try
            {
                Debug.WriteLine($"ToObjectAsync {value}");
                var ret = JsonConvert.DeserializeObject<T>(value);

                if(ret != null) 
                    return ret;

                throw new JsonSerializationException("Deserialized object was null");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Json Deserialization error: {ex.Message}");
                Debug.WriteLine(value);
                throw;
            }
        });
        //return await Task.Run(() =>
        //{
        //    return JsonConvert.DeserializeObject<T>(value);
        //});
    }


    public static async Task<string> StringifyAsync(object value)
    {
        return await Task.Run(() =>
        {
            return JsonConvert.SerializeObject(value);
        });
    }
}
