﻿using System;
using System.Linq;
using Eventstore.ClientAPI.Tests.Helpers;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using NUnit.Framework;

namespace Eventstore.ClientAPI.Tests
{
    [TestFixture, Ignore("TODO: Setup SSL")]
    public class ssl_append_to_stream 
    {
        private readonly TcpType _tcpType = TcpType.Ssl;

        protected virtual IEventStoreConnection BuildConnection()
        {
            var cn = TestConnection.Create(_tcpType);
            return cn;
        }

        [Test, Category("Network")]
        public void should_allow_appending_zero_events_to_stream_with_no_problems()
        {
            const string stream = "should_allow_appending_zero_events_to_stream_with_no_problems";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();
                Assert.AreEqual(-1, store.AppendToStreamAsync(stream, ExpectedVersion.NoStream).Result.NextExpectedVersion);

                var read = store.ReadStreamEventsForwardAsync(stream, 0, 2, resolveLinkTos: false).Result;
                Assert.That(read.Events.Length, Is.EqualTo(0));
            }
        }

        [Test, Category("Network")]
        public void should_create_stream_with_no_stream_exp_ver_on_first_write_if_does_not_exist()
        {
            const string stream = "should_create_stream_with_no_stream_exp_ver_on_first_write_if_does_not_exist";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();
                Assert.AreEqual(0, store.AppendToStreamAsync(stream, ExpectedVersion.NoStream, TestEvent.NewTestEvent()).Result.NextExpectedVersion);

                var read = store.ReadStreamEventsForwardAsync(stream, 0, 2, resolveLinkTos: false);
                Assert.That(read.Result.Events.Length, Is.EqualTo(1));
            }
        }

        [Test]
        [Category("Network")]
        public void should_create_stream_with_any_exp_ver_on_first_write_if_does_not_exist()
        {
            const string stream = "should_create_stream_with_any_exp_ver_on_first_write_if_does_not_exist";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();
                Assert.AreEqual(0, store.AppendToStreamAsync(stream, ExpectedVersion.Any, TestEvent.NewTestEvent()).Result.NextExpectedVersion);

                var read = store.ReadStreamEventsForwardAsync(stream, 0, 2, resolveLinkTos: false);
                Assert.That(read.Result.Events.Length, Is.EqualTo(1));
            }
        }

        [Test]
        [Category("Network")]
        public void should_fail_writing_with_correct_exp_ver_to_deleted_stream()
        {
            const string stream = "should_fail_writing_with_correct_exp_ver_to_deleted_stream";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();

                var delete = store.DeleteStreamAsync(stream, ExpectedVersion.EmptyStream, hardDelete: true);
                Assert.DoesNotThrow(delete.Wait);

                var append = store.AppendToStreamAsync(stream, ExpectedVersion.NoStream, new[] { TestEvent.NewTestEvent() });
                Assert.That(() => append.Wait(), Throws.Exception.TypeOf<AggregateException>().With.InnerException.TypeOf<StreamDeletedException>());
            }
        }

        [Test]
        [Category("Network")]
        public void should_fail_writing_with_any_exp_ver_to_deleted_stream()
        {
            const string stream = "should_fail_writing_with_any_exp_ver_to_deleted_stream";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();

                var delete = store.DeleteStreamAsync(stream, ExpectedVersion.EmptyStream, hardDelete: true);
                Assert.DoesNotThrow(delete.Wait);

                var append = store.AppendToStreamAsync(stream, ExpectedVersion.Any, new[] { TestEvent.NewTestEvent() });
                Assert.That(() => append.Wait(), Throws.Exception.TypeOf<AggregateException>().With.InnerException.TypeOf<StreamDeletedException>());
            }
        }

        [Test]
        [Category("Network")]
        public void should_fail_writing_with_invalid_exp_ver_to_deleted_stream()
        {
            const string stream = "should_fail_writing_with_invalid_exp_ver_to_deleted_stream";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();

                var delete = store.DeleteStreamAsync(stream, ExpectedVersion.EmptyStream, hardDelete: true);
                Assert.DoesNotThrow(delete.Wait);

                var append = store.AppendToStreamAsync(stream, 5, new[] { TestEvent.NewTestEvent() });
                Assert.That(() => append.Wait(), Throws.Exception.TypeOf<AggregateException>().With.InnerException.TypeOf<StreamDeletedException>());
            }
        }

        [Test]
        [Category("Network")]
        public void should_append_with_correct_exp_ver_to_existing_stream()
        {
            const string stream = "should_append_with_correct_exp_ver_to_existing_stream";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();
                Assert.AreEqual(0, store.AppendToStreamAsync(stream, ExpectedVersion.EmptyStream, TestEvent.NewTestEvent()).Result.NextExpectedVersion);
                Assert.AreEqual(1, store.AppendToStreamAsync(stream, 0, TestEvent.NewTestEvent()).Result.NextExpectedVersion);
            }
        }

        [Test]
        [Category("Network")]
        public void should_append_with_any_exp_ver_to_existing_stream()
        {
            const string stream = "should_append_with_any_exp_ver_to_existing_stream";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();
                Assert.AreEqual(0, store.AppendToStreamAsync(stream, ExpectedVersion.EmptyStream, TestEvent.NewTestEvent()).Result.NextExpectedVersion);
                Assert.AreEqual(1, store.AppendToStreamAsync(stream, ExpectedVersion.Any, TestEvent.NewTestEvent()).Result.NextExpectedVersion);
            }
        }

        [Test]
        [Category("Network")]
        public void should_return_log_position_when_writing()
        {
            const string stream = "should_return_log_position_when_writing";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();
                var result = store.AppendToStreamAsync(stream, ExpectedVersion.EmptyStream, TestEvent.NewTestEvent()).Result;
                Assert.IsTrue(0 < result.LogPosition.PreparePosition);
                Assert.IsTrue(0 < result.LogPosition.CommitPosition);
            }
        }

        [Test]
        [Category("Network")]
        public void should_fail_appending_with_wrong_exp_ver_to_existing_stream()
        {
            const string stream = "should_fail_appending_with_wrong_exp_ver_to_existing_stream";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();
                Assert.AreEqual(0, store.AppendToStreamAsync(stream, ExpectedVersion.EmptyStream, TestEvent.NewTestEvent()).Result.NextExpectedVersion);

                var append = store.AppendToStreamAsync(stream, 1, new[] { TestEvent.NewTestEvent() });
                Assert.That(() => append.Wait(), Throws.Exception.TypeOf<AggregateException>().With.InnerException.TypeOf<WrongExpectedVersionException>());
            }
        }

        [Test]
        [Category("Network")]
        public void can_append_multiple_events_at_once()
        {
            const string stream = "can_append_multiple_events_at_once";
            using (var store = BuildConnection())
            {
                store.ConnectAsync().Wait();

                var events = Enumerable.Range(0, 100).Select(i => TestEvent.NewTestEvent(i.ToString(), i.ToString()));
                Assert.AreEqual(99, store.AppendToStreamAsync(stream, ExpectedVersion.EmptyStream, events).Result.NextExpectedVersion);
            }
        }
    }
}