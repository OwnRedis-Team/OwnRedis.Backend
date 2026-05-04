using Microsoft.Extensions.DependencyInjection;
using OwnRedis.Client.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OwnRedis.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOwnRedisClient(
            this IServiceCollection services,
            Action<OwnRedisClientOptions> configure)
        {
            var options = new OwnRedisClientOptions();
            configure(options);

            services.AddSingleton(options);
            services.AddHttpClient<IOwnRedisClient, OwnRedisClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            });

            return services;
        }
    }
}
