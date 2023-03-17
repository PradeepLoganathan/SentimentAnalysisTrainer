using System;
using Microsoft.Extensions.Configuration;

namespace Common;

public class SettingsReader
{
    private IConfiguration _configuration { get; set; }
    public T ReadSection<T>(string sectionName)
    {
        var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
        var _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return _configuration.GetSection(sectionName).Get<T>();
    }
}