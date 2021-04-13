![icon](https://raw.githubusercontent.com/theXappy/ExtcapNet/main/media/icon.png)
# ExtcapNet
A small .NET standard library that implements the [extcap interface](https://www.wireshark.org/docs/man-pages/extcap.html) for you.

## How to include ExtcapNet in your project
There are 2 ways to add the ExtcapNet library to your project:

1. Get it [from NuGet](https://www.nuget.org/packages/ExtcapNet/)\
or
2. Download the code in this repo and add the ExtcapNet project (.csproj) to your solution


## Quick Start
To use the extcap interface you'll need to use the `ExtcapManager` class and it's 2 methods:
1. `ExtcapManager.RegisterInterface()` - To add one or more capturable interfaces to Wireshark's list
2. `ExtcapManager.Run()` - To perform the necessary API communication with Wireshark*

\* Wireshark's extcap interface is based on invoking the plugin executable several  
times at startup/when starting to capture with different command line arguments and  
getting specific results in it's standard output.

The most basic usage for the library is provided in this example
```C#
static void Main(string[] args)
{
    var extcap = new ExtcapManager();

    extcap.RegisterInterface(displayName: "Dummy Interface Name",
                             producer: DummyPacketsProducer,
                             defaultLinkLayer: LinkLayerType.Ethernet);

    // This will handle different invocations by wireshark
    // When finally a capture command arrives this function blocks until 'DummyPacketsProducer'
    // is done/wireshark stops the capturing.
    extcap.Run(args);
}

static void DummyPacketsProducer(Dictionary<ConfigField, string> config, IPacketsPublisher publisher)
{
    // In this function you should continuously read from your packets source
    // and send them to Wireshark using the 'publisher' arg.
    //
    // To keep this example short, we'll simply generate some packets ourselves.
    for (int i = 0; i < 10; i++)
    {
        byte[] newEtherPacket = new byte[14];
        // Setting different first byte of every packet so we can tell them apart
        newEtherPacket[0] = (byte)i;

        publisher.Send(newEtherPacket);
    }
}
```

This code should cover most basic cases.  
The only real missing part from making this code a worthy plugin is replacing the body of the `DummyPacketsProducer` function.

### UDP Dump Look-alike Example
To demonstrate the convinience this library provides, take a look at the following example which attemps to mimik the `udpdump.exe` plugin (bundled with Wireshark):
```C#
static void Main(string[] args)
{
    var extcap = new ExtcapManager();

    extcap.RegisterInterface(displayName: "Fake udpdump",
                             producer: FakeUdpDumpProducer,
                             defaultLinkLayer: LinkLayerType.Ethernet); // TODO: Only supports Ethernet inside UDP
    extcap.Run(args);
}

static void FakeUdpDumpProducer(Dictionary<ConfigField, string> config, IPacketsPublisher publisher)
{
    // Plugin specific logic: Wait for incoming UDP packets
    // when one arrives, just forward it's entire payload as an Ethernet packet to Wireshark

    UdpClient udpListener = new UdpClient(5555); // TODO: Port is hard-coded
    IPEndPoint ipe = new IPEndPoint(0,0);
    while(true) {
        byte[] nextUdpPayload = udpListener.Receive(ref ipe);
        publisher.Send(nextUdpPayload);
    }
}
```
This example works but it is not a complete copy. `udpdump` has a few more features which we are lacking.  
For example, you can specify in the `udpdump`'s settings on which *port to listen*.  
You can also specify the *encapsulated protocol type* so it's dissector will be called by Wireshark.

To allow such flexability in ExtcapNet a deeper dive into the library is required.  
ExtcapNet allows you to define **"configuration fields"** which Wireshark will render in  
a special window for the users to configure the plugin (Like the ones udpdump and sshdump have).

To learn about configuration support, see the ['revemped udpdump example'](https://github.com/theXappy/ExtcapNet/blob/main/revemped_udpdump_example.md)

## Compiling a single .exe (optional)
After you're done developing your plugin you'd want to use it in Wireshark.  
To do so you need to copy everything from the compilation folder (/bin/debug or /bin/release)  
to Wiresharks's 'extcaps' directory.

.NET projects commonly compile to several different files (dlls, exe, config, ...) and copying all  
of those to the directory might make a mess.  
Luckily, .NET core/.NET 5 support [single-file publishing](https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file) which produces only 2 files: `program.exe` and `program.pdb` (A symbols file. Not necessary for execution).  
To publish a single file you can use this command in Visual Studio's "Package Manager Console":  
```
PM> dotnet publish -r win-x64 -c Debug /p:PublishSingleFile=true
```  
(Adjust windows version and Debug/Release according to your needs)

## Thanks
Shark, Puzzle icons icon by [Icons8](https://icons8.com/)
