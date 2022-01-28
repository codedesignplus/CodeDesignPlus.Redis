using CodeDesignPlus.Redis.Option;
using CodeDesignPlus.Redis.Test.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CodeDesignPlus.Redis.Test.Model
{
    /// <summary>
    /// Unit test to <see cref="RedisOptions"/>
    /// </summary>
    public class RedisOptionsTest
    {
        /// <summary>
        /// Validate accessors and data annotations
        /// </summary>
        [Fact]
        public void DefaultDatabase_InvalidRange_Failed()
        {
            // Arrange
            var redisOptions = new RedisOptions()
            {
                DefaultDatabase = -1
            };

            // Act
            var results = redisOptions.Validate();

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, x => x.ErrorMessage.Equals("The field DefaultDatabase must be between 0 and 2147483647.") && x.MemberNames.Contains(nameof(RedisOptions.DefaultDatabase)));
        }

        /// <summary>
        /// Validate accessors and data annotations
        /// </summary>
        [Theory]
        [InlineData("redis-server*-1.codedesignplus.com:6379")]
        [InlineData("1270.0.1:6379")]
        public void EndPoints_InvalidIpOrHostName_Failed(string host)
        {
            // Arrange
            var redisOptions = new RedisOptions();
            redisOptions.EndPoints.Add(host);

            // Act
            var results = redisOptions.Validate();

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, x => x.ErrorMessage.Equals("The field EndPoints is invalid.") && x.MemberNames.Contains(nameof(RedisOptions.EndPoints)));
        }

        /// <summary>
        /// Validate accessors and data annotations
        /// </summary>
        [Theory]
        [InlineData("redis-server-1.codedesignplus.com:6379")]
        [InlineData("127.0.0.1:6379")]
        public void Properties_AccessorsAndDataAnnotations_IsValid(string endpoint)
        {
            // Arrange
            var redisOptions = new RedisOptions()
            {
                AbortOnConnectFail = false,
                AllowAdmin = true,
                AsyncTimeout = 10000,
                Certificate = @"C:\certificate.pfx",
                ChannelPrefix = "Redis - ",
                CheckCertificateRevocation = true,
                ClientName = "CodeDesignPlus.Redis.Client",
                ConfigCheckSeconds = 120,
                ConnectRetry = 4,
                ConnectTimeout = 10000,
                DefaultDatabase = 1,
                HighPrioritySocketThreads = false,
                Password = "mypassword",
                PasswordCertificate = "certpassword",
                ResolveDns = true,
                ServiceName = "mymaster",
                Ssl = true,
                SslHost = "redis-server-1.certificate.com",
                SyncTimeout = 10000,
                User = "myuser"
            };

            redisOptions.EndPoints.Add(endpoint);

            // Act
            var results = redisOptions.Validate();

            // Assert
            Assert.Empty(results);

            Assert.False(redisOptions.AbortOnConnectFail);
            Assert.True(redisOptions.AllowAdmin);
            Assert.Equal(10000, redisOptions.AsyncTimeout);
            Assert.Equal(@"C:\certificate.pfx", redisOptions.Certificate);
            Assert.Equal("Redis - ", redisOptions.ChannelPrefix);
            Assert.True(redisOptions.CheckCertificateRevocation);
            Assert.Equal("CodeDesignPlus.Redis.Client", redisOptions.ClientName);
            Assert.Equal(120, redisOptions.ConfigCheckSeconds);
            Assert.Equal(4, redisOptions.ConnectRetry);
            Assert.Equal(10000, redisOptions.ConnectTimeout);
            Assert.Equal(1, redisOptions.DefaultDatabase);
            Assert.False(redisOptions.HighPrioritySocketThreads);
            Assert.Equal("mypassword", redisOptions.Password);
            Assert.Equal("certpassword", redisOptions.PasswordCertificate);
            Assert.True(redisOptions.ResolveDns);
            Assert.Equal("mymaster", redisOptions.ServiceName);
            Assert.True(redisOptions.Ssl);
            Assert.Equal("redis-server-1.certificate.com", redisOptions.SslHost);
            Assert.Equal(10000, redisOptions.SyncTimeout);
            Assert.Equal("myuser", redisOptions.User);
        }
    }
}
