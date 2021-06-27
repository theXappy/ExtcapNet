## Configuration Fields

Let's add the missing features we specified before to make our "Fake udpdump" more like `udpdump.exe`.  
(A note about 'EXPORT_PDU' follows)
```C#
private const string PORT_CONFIG_NAME = "Port";
private const string PAYLOAD_TYPE_CONFIG_NAME = "Payload_type";

static void Main(string[] args)
{
    var extcap = new ExtcapManager();

    ExtcapNet.CaptureInterface.BasicCaptureInterface bci = new BasicCaptureInterface(displayName: "Fake udpdump",
        producer: FakeUdpDumpProducer,
        defaultLinkLayer: LinkLayerType.WiresharkUpperPdu); // Calls 'EXPORTED_PDU' dissector

    // Adding 'Port' config field
    bci.AddConfigField(new ConfigField(displayName:PORT_CONFIG_NAME, 
                                       type:ConfigField.FieldType.Integer, 
                                       required:true));

    // Adding 'Payload type' config field
    bci.AddConfigField(new ConfigField(displayName:PAYLOAD_TYPE_CONFIG_NAME, 
                                       type:ConfigField.FieldType.String, 
                                       required:true));
    extcap.RegisterInterface(bci);

    extcap.Run(args);
}

private static void FakeUdpDumpProducer(Dictionary<ConfigField, string> configuration, IPacketsPublisher publisher)
{
    // Retrieving information from this specific executaion's configuration
    string portText = configuration.Single(kvp => kvp.Key.DisplayName == PORT_CONFIG_NAME).Value;
    int port = int.Parse(portText);
    string payloadType = configuration.Single(kvp => kvp.Key.DisplayName == PAYLOAD_TYPE_CONFIG_NAME).Value;
    byte[] payloadTypeBytes = Encoding.ASCII.GetBytes(payloadType);

    // Creating 'EXPORT_PDU' header
    byte[] protoNameTag = new byte[4]
    {
        0x00, 0x0c, // TAG Type: PDU content protocol name
        0x00, 0x00, // TAG Length: Place holder for name's length
    };
    protoNameTag[2] = (byte)(payloadTypeBytes.Length / 256);
    protoNameTag[3] = (byte)(payloadTypeBytes.Length % 256);
    byte[] endOfOptionsTag = new byte[4]; // Just 4 zeroes

    byte[] exportHeader = protoNameTag.Concat(payloadTypeBytes)
                                        .Concat(endOfOptionsTag)
                                        .ToArray();

    UdpClient udpListener = new UdpClient(port);
    IPEndPoint ipe = new IPEndPoint(0, 0);
    while (true)
    {
        byte[] nextUdpPayload = udpListener.Receive(ref ipe);
        // Wrap payload in 'EXPORTED_PDU' header
        byte[] completePacket = exportHeader.Concat(nextUdpPayload).ToArray();

        publisher.Send(completePacket);
    }
}
```

The **'EXPORT_PDU'** layer is used to wrap payloads for wireshark.  
Notably it is used by the real `udpdump` to wrap the payloads it recieved so Wireshark knows what dissector to call.  
This layer supports adding several "tags" (in the format of TLV). One of those tags (as used above) specifies the encapsulated payload protocol as a string.  
In case you wondered why this pseudo-layer is used in this example instead of registering the desired protocol in the "`defaultLinkLayer`" argument  
(when registering the interface) it is done because the "`defaultLinkLayer`" is queried by Wireshark from the extcap plugin (whether it is  
`udpdump` or our example) _before_ the configuration is shown to, and set by, the user.

The available configuration field types are:
```C#
public enum FieldType
{
    Integer,
    Unsigned,
    /*may Include Scientific / Special Notation*/
    Long,
    Float,
    /*display Checkbox*/
    Boolean,
    /*display Textbox*/
    String,
    /*display A Textbox With Masked Text*/
    Password,
    /*display Selector Table, All Values As Strings*/
    Selector,
    /*display Group Of Radio Buttons With Provided Values, All Values As Strings*/
    Radio,
    /*display A Textbox For Selecting Multiple Options, Values As Strings*/
    Multicheck,
    /*display A Dialog To Select A File From The Filesystem, Value As String*/
    Fileselect,
    /*display A Calendar*/
    Timestamp
}
```

### Multi-option fields
For multi-options fields (Selector, Radio and Multicheck) instead of using the `ConfigField` class  
the fields should be declared using `MultiOptionsField`.
