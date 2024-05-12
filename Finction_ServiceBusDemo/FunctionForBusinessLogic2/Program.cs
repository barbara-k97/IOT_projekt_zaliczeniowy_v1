using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Text;
using Opc.UaFx.Client;
   
using Microsoft.Azure.Devices;
using Opc.Ua;
using System.Net.Sockets;
using Opc.UaFx;
using Microsoft.Azure.Devices.Common;
using System.Net.NetworkInformation;
using FuncionForBusinessLogic;

const string sbConnectionString = "Endpoint=sb://servicebusme.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=B38lJY8ysnP3BaR0jXmULPQvjE5NSL2Jf+ASbH4HFU4=\r\n";
const string queueName = "kolejka-produkcja";


await using ServiceBusClient client = new ServiceBusClient(sbConnectionString);
await using ServiceBusProcessor processor = client.CreateProcessor(queueName);

processor.ProcessMessageAsync += Processor_ProcessMessageAsync;
processor.ProcessErrorAsync += Processor_ProcessErrorAsync;

await processor.StartProcessingAsync();

Console.WriteLine("Waiting for messages... Press enter to stop.");
Console.ReadLine();

Console.WriteLine("\nStopping the receiver...");
await processor.StopProcessingAsync();
Console.WriteLine("Stopped receiving messages");

async Task Processor_ProcessMessageAsync(ProcessMessageEventArgs arg)
{
     Console.WriteLine($"RECEIVED MESSAGE:\n\t{arg.Message.Body}");
     var message = Encoding.UTF8.GetString(arg.Message.Body);
     ReadMessage mesg = JsonConvert.DeserializeObject<ReadMessage>(message);



     Console.WriteLine(mesg.windowEndTime);
     Console.WriteLine(mesg.DeviceName);
      
     Console.WriteLine(mesg.productionDevice);

      
     string connectionString = "HostName=hubZajecia.azure-devices.net;DeviceId=test_device;SharedAccessKey=x8bzG9iX+bKOOaTd/e8XOeR67UIOud5iwAIoTFLb3WI=\r\n";
     string deviceId = mesg.DeviceName;
     var client = new OpcClient("opc.tcp://localhost:4840/");
     client.Connect();
     var device = new Class1(client);
   
    
          Console.WriteLine("!_________ Zgłoszono wywyłanie metody ProductionDecrease");

          OpcValue ProductionRate = client.ReadNode("ns=2;s=" + deviceId + "/ProductionRate");
          int production = (int)ProductionRate.Value;
          int low_production = production - 10;
          Console.WriteLine($" produkcja obecna {production} , zmniejszona {low_production} ");

          OpcStatus result = client.WriteNode("ns=2;s=" + deviceId + "/ProductionRate", low_production);

   
          Console.WriteLine("!__________________________________________________");

     





     /* string methodName = "EmergencyStop";

      ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

      CloudToDeviceMethod method = new CloudToDeviceMethod(methodName);
      method.ResponseTimeout = TimeSpan.FromSeconds(30);
      CloudToDeviceMethodResult response = await serviceClient.InvokeDeviceMethodAsync(deviceId, method);
      Console.WriteLine($"Response status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
     */

}

Task Processor_ProcessErrorAsync(ProcessErrorEventArgs arg)
{
     Console.WriteLine(arg.Exception.ToString());

     return Task.CompletedTask;
}

public class ReadMessage
{
     public DateTime windowEndTime { get; set; }
     public string DeviceName { get; set; }
  

     public string productionDevice { get; set; }

}
