using CodeDesignPlus.Redis.Extension;
using CodeDesignPlus.Redis.Option;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CodeDesignPlus.Redis.Test.Extensions
{
    /// <summary>
    /// Unit test to <see cref="RedisExtensions"/>
    /// </summary>
    public class RedisExtensionsTest
    {
        /// <summary>
        /// Should return an <see cref="ArgumentNullException"/> when configuration parameter is null
        /// </summary>
        [Fact]
        public void AddRedisService_ConfigurationIsNull_ArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddRedisService(null));
        }

        /// <summary>
        /// Should return an <see cref="ArgumentNullException"/> when services parameter is null
        /// </summary>
        [Fact]
        public void AddRedisService_ParameterIsNull_ArgumentNullException()
        {
            // Arrange
            ServiceCollection services = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddRedisService(null));
        }

        /// <summary>
        /// Should register a <see cref="IRedisService"/> services and <see cref="RedisOptions"/>
        /// </summary>
        [Fact]
        public void AddRedisService_RegisterServices()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { $"{RedisOptions.Section}:{nameof(RedisOptions.Certificate)}", @"C:\certificate.pfx" }
            });

            var configuration = configurationBuilder.Build();

            var services = new ServiceCollection();

            // Act 
            services.AddRedisService(configuration);

            var provider = services.BuildServiceProvider();

            // Assert
            var redisService = services.FirstOrDefault(x => x.ServiceType == typeof(IRedisService) && x.ImplementationType == typeof(RedisService));

            Assert.NotNull(redisService);
            Assert.Equal(ServiceLifetime.Singleton, redisService.Lifetime);

            var options = provider.GetService<IOptions<RedisOptions>>();
            Assert.NotNull(options);

            Assert.Equal(@"C:\certificate.pfx", options.Value.Certificate);
        }
    }
}
