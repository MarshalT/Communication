using System;
using System.Text;
using Xunit;

namespace CommSdk.Tests
{
    public class TransportTests
    {
        [Fact]
        public void SendsTextAsUtf8Bytes()
        {
            var transport = new FakeTransport();
            transport.Open();

            transport.Send("你好");

            Assert.Single(transport.Sent);
            Assert.Equal(Encoding.UTF8.GetBytes("你好"), transport.Sent[0]);
        }

        [Fact]
        public void RejectsNullTextPayload()
        {
            var transport = new FakeTransport();
            transport.Open();

            Assert.Throws<ArgumentNullException>(() => transport.Send((string)null));
        }
    }
}
