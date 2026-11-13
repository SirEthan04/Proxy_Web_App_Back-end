using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;

internal static class ProductionSmoke
{
    public static async Task<int> Run(string apiPath)
    {
        var checks = 0;
        void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); checks++; }
        apiPath = Path.GetFullPath(apiPath);
        Check(File.Exists(apiPath), "No se encontró el binario de API.");
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        ProcessStartInfo StartInfo(string? initialization = null)
        {
            var info = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(apiPath)!
            };
            info.ArgumentList.Add(apiPath);
            info.ArgumentList.Add("--urls"); info.ArgumentList.Add("http://127.0.0.1:" + port);
            if (initialization is not null) info.ArgumentList.Add(initialization);
            info.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
            info.Environment["DOTNET_ENVIRONMENT"] = "Production";
            info.Environment["RENDER"] = "true";
            info.Environment["ConnectionStrings__DefaultConnection"] = "Server=mysql.example.invalid;Port=3306;Database=bodego_deploy_check;User=unused;SslMode=VerifyFull;ConnectionTimeout=1;";
            info.Environment["Jwt__SigningKeyBase64"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            info.Environment["Jwt__Issuer"] = "BodeGo.API"; info.Environment["Jwt__Audience"] = "BodeGo.Vue";
            info.Environment["Cors__AllowedOrigins__0"] = "https://frontend.example";
            info.Environment["ReverseProxy__KnownProxies__0"] = "127.0.0.1";
            info.Environment["Logging__LogLevel__Default"] = "Warning";
            return info;
        }

        using var process = Process.Start(StartInfo()) ?? throw new InvalidOperationException("No se pudo iniciar la API.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:" + port), Timeout = TimeSpan.FromSeconds(2) };
            var ready = false;
            for (var attempt = 0; attempt < 50; attempt++)
            {
                if (process.HasExited) break;
                try { using var response = await http.GetAsync("/api/v1/auth/me"); if (response.StatusCode == HttpStatusCode.Unauthorized) { ready = true; break; } }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }
                await Task.Delay(100);
            }
            Check(ready, "La API Production no respondió 401; no se imprimen logs ni secretos.");
            foreach (var path in new[] { "/scalar", "/scalar/v1", "/openapi/v1.json" })
            {
                using var response = await http.GetAsync(path);
                Check(response.StatusCode == HttpStatusCode.NotFound, "Se expuso documentación en Production.");
            }
            using (var login = await http.PostAsJsonAsync("/api/v1/auth/login", new { }))
                Check(login.StatusCode == HttpStatusCode.BadRequest, "La validación de login cambió.");
            foreach (var path in new[] { "/api/v1/users", "/api/v1/purchases", "/api/v1/sales", "/api/v1/wastes", "/api/v1/inventory-counts" })
            {
                using var response = await http.PostAsJsonAsync(path, new { });
                Check(response.StatusCode == HttpStatusCode.Unauthorized, "Un POST protegido no devolvió 401.");
            }
            foreach (var origin in new[] { "https://frontend.example", "https://not-allowed.example", "http://localhost:5174" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/products");
                request.Headers.Add("Origin", origin);
                request.Headers.Add("Access-Control-Request-Method", "POST");
                request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
                using var response = await http.SendAsync(request);
                if (origin == "https://frontend.example")
                    Check(response.IsSuccessStatusCode && response.Headers.GetValues("Access-Control-Allow-Origin").Single() == origin
                        && response.Headers.GetValues("Access-Control-Allow-Headers").Single().Contains("authorization", StringComparison.OrdinalIgnoreCase),
                        "CORS Production no permite Bearer del frontend.");
                else Check(!response.Headers.Contains("Access-Control-Allow-Origin"), "CORS Production permite un origen ajeno.");
                Check(!response.Headers.Contains("Access-Control-Allow-Credentials"), "Se habilitaron cookies innecesarias.");
            }

            foreach (var flag in new[] { "--initialize-iam-roles", "--initialize-iam-admin" })
            {
                using var command = Process.Start(StartInfo(flag)) ?? throw new InvalidOperationException("No se pudo comprobar el inicializador.");
                var stdout = command.StandardOutput.ReadToEndAsync();
                var stderr = command.StandardError.ReadToEndAsync();
                await command.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
                var text = (await stdout) + (await stderr);
                Check(command.ExitCode != 0 && text.Contains("Development", StringComparison.Ordinal),
                    "Se permitió inicializar IAM desde Production.");
            }
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await output; await error;
        }
        return checks;
    }
}
