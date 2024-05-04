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
               //Console.WriteLine(data);
          /*    / var selectedData = new
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
               //Console.WriteLine(dataString);
               Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
               eventMessage.ContentType = MediaTypeNames.Application.Json;
               eventMessage.ContentEncoding = "utf-8";
               await client.SendEventAsync(eventMessage);
              


          }
          #endregion


          #region Device Twin

          public async Task UpdateTwinAsync()
          {
               var twin = await client.GetTwinAsync();

               Console.WriteLine($"\nInitial twin value received: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");
               Console.WriteLine();

               var reportedProperties = new TwinCollection();
               reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;

               await client.UpdateReportedPropertiesAsync(reportedProperties);
          }

          private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
          {
               Console.WriteLine($"\tDesired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
               Console.WriteLine("\tSending current time as reported property");
               TwinCollection reportedProperties = new TwinCollection();
               reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;

               await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
          }

          #endregion Device Twin




     }
}
