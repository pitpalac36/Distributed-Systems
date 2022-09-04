
1. The communication is done using the Google Protobuffer 3.x messages defined below, over TCP. The exchange will be
   asynchronous. When sending a request/response, open the TCP connection, send the message, then close the connection.
   When listening for requests, get the request, then close the socket.

2. The system consists of several processes and one hub. The processes are implementation of what the textbook refers
   to as a process, with the extension that they can participate in multiple systems. They should be able to route
   messages and events separately for each system that the process is involved into.

3. The hub is responsible of informing the processes of the system(s) they belong to, trigger algorithms, and receive
   notifications that it can use to validate the functionality.

4. Your job is to implement a process that can run the algorithms shown in the evaluation flow below. Use the reference
   binaries provided by your instructor to verify your implementation

5. Process referencing: Upon starting, a process will connect to the hub and register sending: owner alias, process
   index, process host, process listening port (see ProcRegistration). The hub address and port will be configured manually.
