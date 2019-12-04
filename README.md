![](https://github.com/Malcolmnixon/NetDiscovery/workflows/dotnet-windows/badge.svg)
![](https://github.com/Malcolmnixon/NetDiscovery/workflows/dotnet-ubuntu/badge.svg)

# NetDiscovery
Local Network Discovery Library for .NET projects

This project simplifies performing local-network discovery for client and server applications. Server applications can announce their presence on the network, and client applications can find the servers.

# Discovery Servers
Discovery servers can announce their presence as follows:
~~~~
// Create UDP discovery provider (using port 52148)
using var provider = new UdpProvider(52148);

// Create a discovery server advertising the server with the identity of "TestServer"
using var server = provider.CreateServer();
server.Identity = "TestServer";

// Start discovery components
server.Start();
provider.Start();
~~~~

# Discovery Clients
Discovery clients can find servers as follows:
~~~~
// Create UDP discovery provider (using port 52148)
using var provider = new UdpProvider(52148);

// Create a discovery client printing all servers it finds
using var client = provider.CreateClient();
client.Discovery += (s, e) => Console.WriteLine($"Discovered: {e.Address}: {e.Identity}");

// Start discovery components
client.Start();
provider.Start();
~~~~
