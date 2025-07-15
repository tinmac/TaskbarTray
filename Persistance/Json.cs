using Newtonsoft.Json;
using Serilog;
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
                var ret = JsonConvert.DeserializeObject<T>(value);

                if(ret != null) 
                    return ret;

                throw new JsonSerializationException("Deserialized object was null");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Json Deserialization error on value [{value}]: ", ex.Message);

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
