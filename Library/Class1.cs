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

          /*
                    #region D2C - Sending telemetry
                    public async Task SendTelemetry(dynamic data)
                    {
                         Console.WriteLine(data);

                         var selectedData = new
                         {
                              //nazwa = data.name,
                              //ProductionStatus = data.ProductionStatus,
                              WorkorderId = data.WorkorderId,
                              Temperature = data.Temperature,
                              ProductionRate = data.ProductionRate,
                              GoodCount = data.GoodCount,
                              BadCount = data.BadCount,
                         };

                         var dataString = JsonConvert.SerializeObject(selectedData);
                         Console.WriteLine(dataString);
                         Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
                         eventMessage.ContentType = MediaTypeNames.Application.Json;
                         eventMessage.ContentEncoding = "utf-8";
                         await client.SendEventAsync(eventMessage);
                         if (true)
                              await Task.Delay(2000); //20sekund

                         Console.WriteLine(" Koniec wywołania metody SendTelemetry z Library ");

                    }
                    #endregion

          */


          #region D2C - Sending telemetry
          public async Task SendTelemetry(dynamic data)
          {
               Console.WriteLine(data);
          /*     var selectedData = new
               {
                    DeviceName = data.name,
                    WorkorderId = data.WorkorderId,
                    ProductionStatus = data.ProductionStatus,
                    Temperature = data.Temperature,
                    ProductionRate = data.ProductionRate,
                    GoodCount = data.GoodCount,
                    BadCount = data.BadCount,
                    DeviceErrors = data.DeviceErrors,
               };*/

               var dataString = JsonConvert.SerializeObject(data);
               Console.WriteLine(dataString);
               Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
               eventMessage.ContentType = MediaTypeNames.Application.Json;
               eventMessage.ContentEncoding = "utf-8";
               await client.SendEventAsync(eventMessage);
               if (true)
                    await Task.Delay(2000); //2000milisekund = 20sekund 


          }
          #endregion



     }
}
