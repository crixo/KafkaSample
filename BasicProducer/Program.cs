using System;

namespace BasicProducer
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using Confluent.Kafka;
    using Confluent.Kafka.Serialization;

    public class Program
    {
        public static void Main(string[] args)
        {
            //if (args.Length != 2)
            //{
            //    Console.WriteLine("Usage: .. brokerList topicName");
            //    return;
            //}

            string brokerList = "192.168.1.7:9092";
            string topicName = "my-topic";

            var config = new Dictionary<string, object> { 
                { "bootstrap.servers", brokerList },
                //{ "auto.create.topics.enable", true }
            };

            using (var producer = new Producer<Null, string>(config, null, new StringSerializer(Encoding.UTF8)))
            {
                PrintMetadata(brokerList);
                ListGroups(brokerList);
                Console.WriteLine($"{producer.Name} producing on {topicName}. q to exit.");


                string text;
                while ((text = Console.ReadLine()) != "q")
                {
                    if (string.IsNullOrEmpty(text)) continue;
                    var deliveryReport = producer.ProduceAsync(topicName, null, text);
                    deliveryReport.ContinueWith(task =>
                    {
                        Console.WriteLine($"Partition: {task.Result.Partition}, Offset: {task.Result.Offset}");
                    });
                }

                // Tasks are not waited on synchronously (ContinueWith is not synchronous),
                // so it's possible they may still in progress here.
                producer.Flush(TimeSpan.FromSeconds(10));
            }
        }

        static void PrintMetadata(string brokerList)
        {
            var config = new Dictionary<string, object> { { "bootstrap.servers", brokerList } };
            using (var producer = new Producer(config))
            {
                var meta = producer.GetMetadata(true, null);
                Console.WriteLine($"{meta.OriginatingBrokerId} {meta.OriginatingBrokerName}");
                meta.Brokers.ForEach(broker =>
                    Console.WriteLine($"Broker: {broker.BrokerId} {broker.Host}:{broker.Port}"));

                meta.Topics.ForEach(topic =>
                {
                    Console.WriteLine($"Topic: {topic.Topic} {topic.Error}");
                    topic.Partitions.ForEach(partition =>
                    {
                        Console.WriteLine($"  Partition: {partition.PartitionId}");
                        Console.WriteLine($"    Replicas: {string.Join(",", partition.Replicas)}");
                        Console.WriteLine($"    InSyncReplicas: {string.Join(",", partition.InSyncReplicas)}");
                    });
                });
            }
        }

        static void ListGroups(string brokerList)
        {
            var config = new Dictionary<string, object> { { "bootstrap.servers", brokerList } };

            using (var producer = new Producer(config))
            {
                var groups = producer.ListGroups(TimeSpan.FromSeconds(10));
                Console.WriteLine($"Consumer Groups:");
                foreach (var g in groups)
                {
                    Console.WriteLine($"  Group: {g.Group} {g.Error} {g.State}");
                    Console.WriteLine($"  Broker: {g.Broker.BrokerId} {g.Broker.Host}:{g.Broker.Port}");
                    Console.WriteLine($"  Protocol: {g.ProtocolType} {g.Protocol}");
                    Console.WriteLine($"  Members:");
                    foreach (var m in g.Members)
                    {
                        Console.WriteLine($"    {m.MemberId} {m.ClientId} {m.ClientHost}");
                        Console.WriteLine($"    Metadata: {m.MemberMetadata.Length} bytes");
                        Console.WriteLine($"    Assignment: {m.MemberAssignment.Length} bytes");
                    }
                }
            }
        }
    }
}
