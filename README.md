Build: [![Build Status](https://dev.azure.com/sternheim/Privat/_apis/build/status/jasase.deconz-to-mqtt?branchName=main)](https://dev.azure.com/sternheim/Privat/_build/latest?definitionId=25&branchName=main)

Docker: [![Docker](https://images.microbadger.com/badges/version/jasase/deconztomqtt.svg)](https://microbadger.com/images/jasase/deconztomqtt "Get your own version badge on microbadger.com")

# deconz-to-mqtt

Docker container to push events from deCONZ REST API to a MQTT broker.



## Running

### CLI

```bash
docker run -d \
	--name deconztomqtt \
	--restart=always \
	-e DeconzToMqtt_DeconzAddress=192.168.1.1 \
	-e DeconzToMqtt_DeconzApiKey=528BAC1B40 \
	-e DeconzToMqtt_MqttAddress=192.168.1.10 \
	jasase/deconztomqtt
```

### Environment Variables

| Variable | Optional | Description | 
| -------- | -------- | ----------- |
| DeconzToMqtt_DeconzAddress | | IP or Hostname of deCONZ Host |
| DeconzToMqtt_DeconzApiKey  | | Acquire a valid API Key from deCONZ REST API to authorize the access: https://dresden-elektronik.github.io/deconz-rest-doc/getting_started/#acquire-an-api-key   |
| DeconzToMqtt_DeconzWebsocketPort | x | Port of the Websocket Server. Default: 443 https://dresden-elektronik.github.io/deconz-rest-doc/endpoints/websocket/#websocket-configuration   |
| DeconzToMqtt_DeconzApiPort  | x | Port of the API webserver of deCONZ. Default: 80   |
| DeconzToMqtt_MqttAddress  | | Password to login to MQTT Broker.  |
| DeconzToMqtt_MqttUsername  | x | Username to login to MQTT Broker. Leave blank if MQTT Broker doesn't  |
| DeconzToMqtt_MqttPassword  | x | Password to login to MQTT Broker. |

### Docker-Compose

```yaml
version: '3'
services:
  deconzmqtt:
    image: jasase/deconztomqtt
    restart: "always"
    environment:
      - DeconzToMqtt_DeconzAddress=deconz.local
      - DeconzToMqtt_DeconzApiKey=528BAC1B40
      - DeconzToMqtt_MqttAddress=mqtt.local
      - DeconzToMqtt_MqttUsername=mqttuser
      - DeconzToMqtt_MqttPassword=123456
```

## MQTT Topic

Following the topic hierachy of the messages send by deconz-to-mqtt are send will be explained.

| Level | content | Description | 
| ----- | ------- | ----------- |
| 1 | deconz | Fixed prefix |
| 2 | `tele` or `stat` | Message type |
| 3 | `sensor` or `light` | Item type in deCONZ. |
| 4 | nameOfLigt | Name of the item this message is related to |
| 5 | | Message type related |

### State message

State message are send every time an update via the websocket from deCONZ REST API is received. For every state value of an item a separate message is send. The message contains the value of the state property.
For more information see description of state message field: https://dresden-elektronik.github.io/deconz-rest-doc/endpoints/websocket/#message-fields

Message topic example:
```
deconz/stat/sensor/env_5/pressure
```

Message content example:
```
997
```

### Telemetry message

Telemetry message are send every 60 seconds. The message containing the information you get also if you request the item via REST API. Currently the request is limited to the fields: Id, ETag, ManufacturerName, ModelId, Name, UniqueId, Config, State.

Message topic example:
```
deconz/tele/sensor/env_5/state
```

Message content example:
```JSON
{"Config":{"On":true,"Reachable":true,"Battery":85},"State":{"LastUpdated":"2020-12-21T19:57:52.453","pressure":996},"Type":"ZHAPressure","Id":18,"ETag":"80b049cb769906dd3ad9eaa5858e6a73","ManufacturerName":"LUMI","ModelId":"lumi.weather","Name":"env_5","UniqueId":"00:15:8d:00:02:45:a7:b1-01-0403"}
```
