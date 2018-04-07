using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;


namespace SimpleConsumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Consumer_Seek("192.168.1.7:9092", "my-topic", "my-topic");
            IConfiguration config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
            
            Test1(config);
        }

        public static void Test1(IConfiguration config)
        {
            var conf = new Dictionary<string, object>
    {
      { "group.id", "test-consumer-group-02" },
      { "bootstrap.servers", "192.168.1.7:9092" },
      { "auto.commit.interval.ms", 5000 },
                { "auto.offset.reset", "earliest" }
    };

            using (var consumer = new Consumer<Null, string>(conf, null, new StringDeserializer(Encoding.UTF8)))
            {
                


                consumer.OnMessage += (_, msg)
                  => Console.WriteLine($"Read '{msg.Value}' from: {msg.TopicPartitionOffset}");

                consumer.OnError += (_, error)
                  => Console.WriteLine($"Error: {error}");

                consumer.OnConsumeError += (_, msg)
                  => Console.WriteLine($"Consume error ({msg.TopicPartitionOffset}): {msg.Error}");

                if(config["OFFSET"]!=null)
                {
                    Console.WriteLine(config["OFFSET"]);

                    long offsetFromConfig;
                    var offset = long.TryParse(config["OFFSET"], out offsetFromConfig) ? offsetFromConfig : 0;
                    consumer.Assign(new TopicPartitionOffset[] { new TopicPartitionOffset("my-topic", 0, offset) });
                    consumer.Seek(new TopicPartitionOffset(new TopicPartition("my-topic", 0), new Offset(offset))); 
                }
                else
                {
                    consumer.Subscribe("my-topic"); 
                }


                while (true)
                {
                    consumer.Poll(TimeSpan.FromMilliseconds(100));
                }
            }
        }

        public static void Consumer_Seek(string bootstrapServers, string singlePartitionTopic, string partitionedTopic)
        {
            var consumerConfig = new Dictionary<string, object>
            {
                { "group.id",  "test"},//Guid.NewGuid().ToString()
                { "acks", "all" },
                { "bootstrap.servers", bootstrapServers }
            };

            var producerConfig = new Dictionary<string, object> { { "bootstrap.servers", bootstrapServers } };

            using (var producer = new Producer<Null, string>(producerConfig, null, new StringSerializer(Encoding.UTF8)))
            using (var consumer = new Consumer<Null, string>(consumerConfig, null, new StringDeserializer(Encoding.UTF8)))
            {
                consumer.OnError += (_, e)
                    => Console.WriteLine($"Error: {e}");

                consumer.OnConsumeError += (_, e) =>
                    Console.WriteLine($"Error: {e}");

                const string checkValue = "check value";
                var dr = producer.ProduceAsync(singlePartitionTopic, null, checkValue).Result;
                var dr2 = producer.ProduceAsync(singlePartitionTopic, null, "second value").Result;
                var dr3 = producer.ProduceAsync(singlePartitionTopic, null, "third value").Result;

                Console.WriteLine(dr.Offset);

                consumer.Assign(new TopicPartitionOffset[] { new TopicPartitionOffset(singlePartitionTopic, 0, dr.Offset) });

                Message<Null, string> message;
                Console.WriteLine(consumer.Consume(out message, TimeSpan.FromSeconds(30)));
                Console.WriteLine(consumer.Consume(out message, TimeSpan.FromSeconds(30)));
                Console.WriteLine(consumer.Consume(out message, TimeSpan.FromSeconds(30)));
                consumer.Seek(dr.TopicPartitionOffset);
                Console.WriteLine($"Topic: {dr.TopicPartitionOffset.Topic} - Partiotion: {dr.TopicPartitionOffset.Partition} - Offset: {dr.TopicPartitionOffset.Offset}");

                Console.WriteLine(consumer.Consume(out message, TimeSpan.FromSeconds(30)));
                Console.WriteLine(checkValue + "---" + message.Value);
            }
        }
    }
}

