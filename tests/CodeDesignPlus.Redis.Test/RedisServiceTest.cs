using CodeDesignPlus.Redis.Option;
using CodeDesignPlus.Redis.Test.Helpers.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Xunit;

namespace CodeDesignPlus.Redis.Test
{
    /// <summary>
    /// Unit test to <see cref="RedisService"/>
    /// </summary>
    public class RedisServiceTest
    {
        /// <summary>
        /// Options to control serialization behavior.
        /// </summary>
        private readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        /// <summary>
        /// Options for the Redis service 
        /// </summary>
        private readonly IOptions<RedisOptions> options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisServiceTest"/>
        /// </summary>
        public RedisServiceTest()
        {
            this.options = CreateOptions("aorus with password.pfx", "Temporal1");
        }

        /// <summary>
        /// Should return an <see cref="ArgumentNullException"/> when options is null
        /// </summary>
        [Fact]
        public void Constructor_OptionsIsNull_ArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RedisService(null, null));
        }

        /// <summary>
        /// Should return an <see cref="ArgumentNullException"/> when options is null
        /// </summary>
        [Fact]
        public void Constructor_LoggerIsNull_ArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RedisService(this.options, null));
        }

        /// <summary>
        /// Should connect with redis server and register events
        /// </summary>
        [Fact]
        public void Initialize_Connection_Success()
        {
            // Arrange
            var logger = Mock.Of<ILogger<RedisService>>();

            // Act
            var redisService = new RedisService(this.options, logger);

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);
        }


        /// <summary>
        /// Should connect with redis server and register events
        /// </summary>
        [Fact]
        public void Initialize_CertificateWithPassword_Success()
        {
            // Arrange
            var logger = Mock.Of<ILogger<RedisService>>();

            var optionsRedis = CreateOptions("aorus with password.pfx", "Temporal1");

            // Act
            var redisService = new RedisService(optionsRedis, logger);

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);
        }

        /// <summary>
        /// Should connect with redis server and register events
        /// </summary>
        [Fact]
        public void Initialize_CertificateWithoutPassword_Success()
        {
            // Arrange
            var logger = Mock.Of<ILogger<RedisService>>();

            var optionsRedis = CreateOptions("aorus without password.pfx");

            // Act
            var redisService = new RedisService(optionsRedis, logger);

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);
        }

        /// <summary>
        /// Should connect with redis server and publish event and listener event to validate certificate
        /// </summary>
        [Fact]
        public void ValidationCertificate_CAInstalledTrustStore_Success()
        {
            // Arrange
            var logger = Mock.Of<ILogger<RedisService>>();

            var path = Directory.GetCurrentDirectory();

            var certificate = Path.Combine(path, "Helpers", "Certificate", "root ca.crt");

            if (!File.Exists(certificate))
                throw new InvalidOperationException("Can't run unit test because certificate does not exist");

            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(certificate)));
            store.Close();

            var optionsRedis = CreateOptions("aorus without password.pfx");

            // Act
            var redisService = new RedisService(optionsRedis, logger);

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);

            store.Open(OpenFlags.ReadWrite);
            store.Remove(new X509Certificate2(X509Certificate.CreateFromCertFile(certificate)));
            store.Close();
        }

        /// <summary>
        /// Should connect with redis server and invoke the handler register to <see cref="IConnectionMultiplexer.InternalError"/>
        /// </summary>
        [Fact]
        public void InternalError_WriteLogger()
        {
            // Arrange
            var logger = new Mock<ILogger<RedisService>>();

            var redisService = new RedisService(this.options, logger.Object);

            var endpoint = redisService.Connection.GetEndPoints().FirstOrDefault();

            var arguments = new InternalErrorEventArgs(this, endpoint, ConnectionType.Subscription, new Exception(), nameof(RedisServiceTest));

            var data = new
            {
                arguments.ConnectionType,
                EndPoint = arguments.EndPoint.ToString(),
                arguments.Origin
            };

            // Act
            this.InvokeHandler(redisService, arguments, nameof(IConnectionMultiplexer.InternalError));

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);

            logger.VerifyLogging(string.Format("Internal Error - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions)), LogLevel.Critical);
        }

        /// <summary>
        /// Should connect with redis server and invoke the handler register to <see cref="IConnectionMultiplexer.HashSlotMoved"/>
        /// </summary>
        [Fact]
        public void HashSlotMoved_WriteLogger()
        {
            // Arrange
            var logger = new Mock<ILogger<RedisService>>();

            var redisService = new RedisService(this.options, logger.Object);

            var oldEndpoint = redisService.Connection.GetEndPoints().ElementAtOrDefault(0);
            var newEndpoint = redisService.Connection.GetEndPoints().ElementAtOrDefault(1);

            var arguments = new HashSlotMovedEventArgs(this, 0, oldEndpoint, newEndpoint);

            var data = new
            {
                arguments.HashSlot,
                OldEndPoint = arguments.OldEndPoint.ToString(),
                NewEndPoint = arguments.NewEndPoint.ToString()
            };

            // Act
            this.InvokeHandler(redisService, arguments, nameof(IConnectionMultiplexer.HashSlotMoved));

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);

            logger.VerifyLogging(string.Format("Hash Slot Moved - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions)), LogLevel.Warning);
        }

        /// <summary>
        /// Should connect with redis server and invoke the handler register to <see cref="IConnectionMultiplexer.ErrorMessage"/>
        /// </summary>
        [Fact]
        public void ErrorMessage_WriteLogger()
        {
            // Arrange
            var logger = new Mock<ILogger<RedisService>>();

            var redisService = new RedisService(this.options, logger.Object);

            var endpoint = redisService.Connection.GetEndPoints().ElementAtOrDefault(0);

            var arguments = new RedisErrorEventArgs(this, endpoint, "Internal Error Message");

            var data = new
            {
                EndPoint = arguments.EndPoint.ToString(),
                arguments.Message
            };

            // Act
            this.InvokeHandler(redisService, arguments, nameof(IConnectionMultiplexer.ErrorMessage));

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);

            logger.VerifyLogging(string.Format("Error Message - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions)), LogLevel.Error);
        }

        /// <summary>
        /// Should connect with redis server and invoke the handler register to <see cref="IConnectionMultiplexer.ConnectionRestored"/>
        /// </summary>
        [Fact]
        public void ConnectionRestored_WriteLogger()
        {
            // Arrange
            var logger = new Mock<ILogger<RedisService>>();

            var redisService = new RedisService(this.options, logger.Object);

            var endpoint = redisService.Connection.GetEndPoints().ElementAtOrDefault(0);

            var arguments = new ConnectionFailedEventArgs(this, endpoint, ConnectionType.Subscription, ConnectionFailureType.SocketClosed, new Exception(), nameof(RedisServiceTest));

            var data = new
            {
                arguments.ConnectionType,
                EndPoint = arguments.EndPoint.ToString(),
                arguments.FailureType,
                physicalNameConnection = arguments.ToString()
            };

            // Act
            this.InvokeHandler(redisService, arguments, nameof(IConnectionMultiplexer.ConnectionRestored));

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);

            logger.VerifyLogging(string.Format("Connection Restored - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions)), LogLevel.Information);
        }

        /// <summary>
        /// Should connect with redis server and invoke the handler register to <see cref="IConnectionMultiplexer.ConnectionFailed"/>
        /// </summary>
        [Fact]
        public void ConnectionFailed_WriteLogger()
        {
            // Arrange
            var logger = new Mock<ILogger<RedisService>>();

            var redisService = new RedisService(this.options, logger.Object);

            var endpoint = redisService.Connection.GetEndPoints().ElementAtOrDefault(0);

            var arguments = new ConnectionFailedEventArgs(this, endpoint, ConnectionType.Subscription, ConnectionFailureType.SocketClosed, new Exception(), nameof(RedisServiceTest));

            var data = new
            {
                arguments.ConnectionType,
                EndPoint = arguments.EndPoint.ToString(),
                arguments.FailureType,
                physicalNameConnection = arguments.ToString()
            };

            // Act
            this.InvokeHandler(redisService, arguments, nameof(IConnectionMultiplexer.ConnectionFailed));

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);

            logger.VerifyLogging(string.Format("Connection Failed - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions)), LogLevel.Information);
        }

        /// <summary>
        /// Should connect with redis server and invoke the handler register to <see cref="IConnectionMultiplexer.ConfigurationChangedBroadcast"/>
        /// </summary>
        [Fact]
        public void ConfigurationChangedBroadcast_WriteLogger()
        {
            // Arrange
            var logger = new Mock<ILogger<RedisService>>();

            var redisService = new RedisService(this.options, logger.Object);

            var endpoint = redisService.Connection.GetEndPoints().ElementAtOrDefault(0);

            var arguments = new EndPointEventArgs(this, endpoint);

            var data = new
            {
                EndPoint = arguments.EndPoint.ToString()
            };

            // Act
            this.InvokeHandler(redisService, arguments, nameof(IConnectionMultiplexer.ConfigurationChangedBroadcast));

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);

            logger.VerifyLogging(string.Format("Configuration Changed Broadcast - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions)), LogLevel.Information);
        }

        /// <summary>
        /// Should connect with redis server and invoke the handler register to <see cref="IConnectionMultiplexer.ConfigurationChanged"/>
        /// </summary>
        [Fact]
        public void ConfigurationChanged_WriteLogger()
        {
            // Arrange
            var logger = new Mock<ILogger<RedisService>>();

            var redisService = new RedisService(this.options, logger.Object);

            var endpoint = redisService.Connection.GetEndPoints().ElementAtOrDefault(0);

            var arguments = new EndPointEventArgs(this, endpoint);

            var data = new
            {
                EndPoint = arguments.EndPoint.ToString()
            };

            // Act
            this.InvokeHandler(redisService, arguments, nameof(IConnectionMultiplexer.ConfigurationChanged));

            // Assert
            Assert.True(redisService.IsConnected);
            Assert.NotNull(redisService.Database);
            Assert.NotNull(redisService.Subscriber);

            logger.VerifyLogging(string.Format("Configuration Changed - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions)), LogLevel.Information);
        }

        /// <summary>
        /// Invoke method register in event handler
        /// </summary>
        /// <typeparam name="TEventArgs">Type Event Arguments</typeparam>
        /// <param name="redisService">Redis Service</param>
        /// <param name="arguments">Event Arguments</param>
        /// <param name="member">Name of the property to get handler</param>
        private void InvokeHandler<TEventArgs>(IRedisService redisService, TEventArgs arguments, string member)
        {
            var typeConnection = redisService.Connection.GetType();

            var field = typeConnection.GetField(member, BindingFlags.Instance | BindingFlags.NonPublic);

            var eventHandler = (EventHandler<TEventArgs>)field.GetValue(redisService.Connection);

            eventHandler?.Invoke(this, arguments);
        }

        /// <summary>
        /// Create the options to redis service
        /// </summary>
        /// <param name="pathCertificate">File PFX</param>
        /// <param name="password">Password PFX</param>
        /// <exception cref="InvalidOperationException">Certificate not exist</exception>
        private static IOptions<RedisOptions> CreateOptions(string pathCertificate, string password = null)
        {
            var path = Directory.GetCurrentDirectory();

            var certificate = Path.Combine(path, "Helpers", "Certificate", pathCertificate);

            if (!File.Exists(certificate))
                throw new InvalidOperationException("Can't run unit test because certificate does not exist");

            var redisOptions = new RedisOptions()
            {
                Certificate = certificate,
                Password = "0u193OmSGmxDS4y28Fe1tWS6QwVlkUIlu4BdzKWwDkkNYhpCn/5il7XPECqHnekoc3zIxviuuBFysGlr",
                ResolveDns = false
            };

            if (!string.IsNullOrEmpty(password))
                redisOptions.PasswordCertificate = password;

            redisOptions.EndPoints.Add("192.168.20.45:6379");
            redisOptions.EndPoints.Add("192.168.20.44:6379");
            redisOptions.EndPoints.Add("192.168.20.43:6379");

            return Options.Create(redisOptions);
        }
    }
}
