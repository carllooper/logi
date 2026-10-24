namespace ConsoleDeck.Protocol
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    // One message between the ConsoleDeck plugin and the ConsoleDeck app.
    // Messages are sent as JSON, one per line, over a TCP connection on the loopback address.
    // The app listens, and the plugin connects (and reconnects) to it.
    //
    // Plugin -> app (inputs from the hardware):
    //   { "type": "press",   "id": "key3" }        key or button pressed (or a dial pressed)
    //   { "type": "release", "id": "key3" }        key or button released, when the device reports it
    //   { "type": "turn",    "id": "dial", "delta": -2 }   dial or roller turned by `delta` ticks
    //
    // App -> plugin (feedback shown on the hardware):
    //   { "type": "state", "id": "key3", "label": "ON", "color": "#2E7D32" }
    //   { "type": "state", "id": "dial", "label": "42" }
    //
    // Control ids: key1..key9 (keypad), button1..button4, dial and roller (dialpad).

    public sealed class DeckMessage
    {
        public const Int32 Port = 47800;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public String Type { get; set; }

        public String Id { get; set; }

        public Int32? Delta { get; set; }

        public String Label { get; set; }

        public String Color { get; set; }

        public static DeckMessage Press(String id) => new DeckMessage { Type = "press", Id = id };

        public static DeckMessage Release(String id) => new DeckMessage { Type = "release", Id = id };

        public static DeckMessage Turn(String id, Int32 delta) => new DeckMessage { Type = "turn", Id = id, Delta = delta };

        public static DeckMessage State(String id, String label, String color = null) =>
            new DeckMessage { Type = "state", Id = id, Label = label, Color = color };

        public String ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        // Returns null for lines that are not valid messages, so a bad line never breaks the connection.
        public static DeckMessage FromJson(String line)
        {
            try
            {
                return JsonSerializer.Deserialize<DeckMessage>(line, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
