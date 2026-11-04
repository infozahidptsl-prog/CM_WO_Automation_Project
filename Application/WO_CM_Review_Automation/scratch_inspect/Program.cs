using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Collections.Generic;

namespace scratch_inspect;

class Program
{
    static void Main()
    {
        var conn = new SqlConnection("Server=.\\SQLEXPRESS;Database=WO_CM_Review_Automation;Trusted_Connection=True;TrustServerCertificate=True;");
        conn.Open();
        var cmd = new SqlCommand("SELECT TOP 1 RawWorkOrders FROM UploadHistories ORDER BY UploadDate DESC;", conn);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var val = reader.GetValue(0);
            System.Console.WriteLine($"Type: {val.GetType()}");
            if (val is string json)
            {
                var list = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
                if (list != null && list.Count > 0)
                {
                    foreach(var key in list[0].Keys) {
                        System.Console.WriteLine($"KEY: '{key}'");
                    }
                }
            }
        }
    }
}
