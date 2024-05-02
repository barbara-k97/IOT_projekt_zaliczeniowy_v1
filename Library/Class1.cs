using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Net.Mime;
using System.Text;
using Microsoft.Azure.Devices.Shared;


namespace Library
{
     public class Class1
     {
          private DeviceClient client;
          private OpcClient OPC;


          public Class1(DeviceClient deviceClient, OpcClient OPC)
          {
               this.client = deviceClient;
               this.OPC = OPC;
          }


       

          public async Task InitializeHandlers()
          {
               

               Console.WriteLine("   wywołania metody InitializeHandlers z Library ");

          }

     }
}
