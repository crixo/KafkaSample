# KafkaSample

- Modify docker-compose.yml providing your local machine IP for KAFKA_ADVERTISED_HOST_NAME and ZOOKEEPER_IP=192.168.1.7  
127.0.0.1 does not work.

- Modify docker-compose.yml for the `consumer` service in case you'd like to start conuming from a specific offest

- Start first zookeeper & kafka

```
docker-compose up zookeeper kafka consumer
```

- then the producer. Once the console will be available waiting for input, write any string to send a message

```
docker-compose run producer
```



