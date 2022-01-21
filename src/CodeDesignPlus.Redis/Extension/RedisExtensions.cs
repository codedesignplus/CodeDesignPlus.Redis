using CodeDesignPlus.Redis.Option;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeDesignPlus.Redis.Extension
{
    /// <summary>
    /// Provides extension methods to register library services
    /// </summary>
    public static class RedisExtensions
    {
        /// <summary>
        /// Adds services for redis to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <returns>The same service collection so that multiple calls can be chained.</returns>
        public static IServiceCollection AddRedisService(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(RedisOptions.Section);

            services.AddOptions<RedisOptions>().Bind(section).ValidateDataAnnotations();

            services.AddSingleton<IRedisService, RedisService>();

            return services;
        }
    }
}
