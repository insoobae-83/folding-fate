using NUnit.Framework;
using R3;
using FoldingFate.Infrastructure.EventBus;

namespace FoldingFate.Tests.EditMode.Infrastructure
{
    public struct TestEvent
    {
        public int Value;
        public TestEvent(int value) { Value = value; }
    }

    public struct AnotherEvent
    {
        public string Message;
        public AnotherEvent(string message) { Message = message; }
    }

    [TestFixture]
    public class EventBusTests
    {
        private EventBus _bus;

        [SetUp]
        public void SetUp() { _bus = new EventBus(); }

        [TearDown]
        public void TearDown() { _bus.Dispose(); }

        [Test]
        public void Publish_SubscriberReceivesEvent()
        {
            int received = -1;
            _bus.Receive<TestEvent>().Subscribe(e => received = e.Value);
            _bus.Publish(new TestEvent(42));
            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_MultipleSubscribers_AllReceive()
        {
            int count = 0;
            _bus.Receive<TestEvent>().Subscribe(_ => count++);
            _bus.Receive<TestEvent>().Subscribe(_ => count++);
            _bus.Publish(new TestEvent(1));
            Assert.AreEqual(2, count);
        }

        [Test]
        public void Publish_DifferentTypes_Independent()
        {
            int testReceived = 0;
            string anotherReceived = null;
            _bus.Receive<TestEvent>().Subscribe(e => testReceived = e.Value);
            _bus.Receive<AnotherEvent>().Subscribe(e => anotherReceived = e.Message);
            _bus.Publish(new TestEvent(10));
            Assert.AreEqual(10, testReceived);
            Assert.IsNull(anotherReceived);
        }

        [Test]
        public void Receive_NoPublish_NoCallback()
        {
            int received = -1;
            _bus.Receive<TestEvent>().Subscribe(e => received = e.Value);
            Assert.AreEqual(-1, received);
        }
    }
}