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


          #region D2C - Sending telemetry
          public async Task SendTelemetry(dynamic data)
          {
               Console.WriteLine(" Wywołana metoda SendTelemetry z Library ");
               // tworzenie wlasnego szablony do JSON do Azure IOT Previev 
               var chooseJOSNvalue = new
               {
                    deviceID = data.name.value,
                    ProductionStatus = data.ProductionStatus.Value,
                    WorkorderId = data.WorkorderIdStatus,
                    GoodCount = data.GoodCount,
                    BadCount = data.BadCount,
                    Temperature = data.Temperature,
               };


               var dataString = JsonConvert.SerializeObject(data);

               Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
               eventMessage.ContentType = MediaTypeNames.Application.Json;
               eventMessage.ContentEncoding = "utf-8";
               await client.SendEventAsync(eventMessage);
               if (true)
                    await Task.Delay(20000); // co 20 sekund

               Console.WriteLine(" Koniec wywołania metody SendTelemetry z Library ");

          }
          #endregion




          public async Task InitializeHandlers()
          {
               

               Console.WriteLine("   wywołania metody InitializeHandlers z Library ");

          }

     }
}
