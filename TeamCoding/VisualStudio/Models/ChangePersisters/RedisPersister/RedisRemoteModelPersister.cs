﻿using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamCoding.Documents;
using TeamCoding.Extensions;

namespace TeamCoding.VisualStudio.Models.ChangePersisters.RedisPersister
{
    public class RedisRemoteModelPersister : RemoteModelPersisterBase
    {
        public const string ModelPersisterChannel = "TeamCoding.ModelPersister";
        private readonly Task SubscribeTask;
        public RedisRemoteModelPersister()
        {
            SubscribeTask = TeamCodingPackage.Current.Redis.SubscribeAsync(ModelPersisterChannel, Redis_RemoteModelReceived).HandleException();
        }
        private void Redis_RemoteModelReceived(RedisChannel channel, RedisValue value)
        {
            using (var ms = new MemoryStream(value))
            {
                OnRemoteModelReceived(ProtoBuf.Serializer.Deserialize<RemoteIDEModel>(ms));
            }
        }
        public override void Dispose()
        {
            Task.WaitAll(SubscribeTask);
            base.Dispose();
        }
    }
}