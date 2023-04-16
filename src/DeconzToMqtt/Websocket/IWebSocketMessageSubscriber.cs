using DeconzToMqtt.Model;

namespace DeconzToMqtt.Websocket
{
    public interface IWebSocketMessageSubscriber
    {
        void Handle(WebsocketEvent message);
    }
}
