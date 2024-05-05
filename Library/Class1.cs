using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Net.Mime;
using System.Text;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Exceptions;
using System.Net.Sockets;


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

               //Console.WriteLine("L.błedów " ,  data.DeviceErrors);


               var dataString = JsonConvert.SerializeObject(data);
               //Console.WriteLine(dataString);
               Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
               eventMessage.ContentType = MediaTypeNames.Application.Json;
               eventMessage.ContentEncoding = "utf-8";
               await client.SendEventAsync(eventMessage);
              


          }
          #endregion



          #region SendMessage

          public async Task SendMessage(dynamic data)
          {
             
               var twin = await client.GetTwinAsync();
              


          }

          #endregion


          #region SendMessageToIOT

          public async Task SendMessageToIOT(dynamic data)
          {
               var messageBody = JsonConvert.SerializeObject(data);  // konwersja z string na json
               Message message = new Message(Encoding.UTF8.GetBytes(messageBody));  //opbiekt wiadomosc zmienia sie na bajty w kodowaniu utf8

               // zmiana na typ kontentu jako json
               message.ContentType = MediaTypeNames.Application.Json;
               //kodowanie ustaic na utf8
               message.ContentEncoding = "utf-8";
               //wysłanie wiadomości 
               await client.SendEventAsync(message);
          }

          #endregion

          

          #region Device Twin
          public async Task SetTwinAsync(string deviceName , int deviceError, int prodRate)
          {
               var twin = await client.GetTwinAsync();
               Console.WriteLine();

               //object nameDevice = data.name; 
               var name = deviceName.Replace(" ", "");  // usuń spacje z nazwy 

               //nazwy do JSON
               var device_error = name + "_numer_bledu";
               var device_production = name + "_production_procent";
               // wartosci 
               var device_error_count = deviceError;
               var device_production_count = prodRate;

               

               var reportedProperties = new TwinCollection();
               reportedProperties[device_error] = device_error_count;
               reportedProperties[device_production] = device_production_count;
             

               await client.UpdateReportedPropertiesAsync(reportedProperties);
               //Console.WriteLine($"{DateTime.Now}> Device Twin value was set.");

          }

          public async Task UpdateTwinAsync(string deviceName, int deviceError, int prodRate)
          {
               var twin = await client.GetTwinAsync();

               var reportedProp = twin.Properties.Reported;    //reportet - wartosc na  maszynie
               var desiredProp = twin.Properties.Desired;         // desired - wartosc  ustawiamy na maszynie

               //object nameDevice = data.name; 
               var name = deviceName.Replace(" ", "");  // usuń spacje z nazwy 

               //nazwy do JSON
               var device_error = name + "_numer_bledu";
               var device_production = name + "_production_procent";
               // wartosci 
               var device_error_count = deviceError;
               var device_production_count = prodRate;
               Console.WriteLine("error :", device_error, " , ", device_error_count);

               if (reportedProp.Contains(device_error))
               {
                    var errorInTgisMoment = reportedProp[device_error];
                    // jak błąd jest inny
                    if (errorInTgisMoment != device_error_count)
                    {

                         // zmienił się błąd , trzeba wyświeylić informacje
                         var updateProp = new TwinCollection();
                         updateProp[device_error] = device_error_count;
                         try
                         {
                              await client.UpdateReportedPropertiesAsync(updateProp);
                              Console.WriteLine("Zaktualowano liczbe bledów dla :   ", device_error, ".");
                              Console.WriteLine($"{DateTime.Now}> Device Twin   was update.");
                         }
                         catch (IotHubException ex)
                         {
                              Console.WriteLine("Blad podczas zmiany wartosci bledu", device_error);
                         }


                    }
                    else
                    {
                         Console.WriteLine(" Brak zmiany bledu - nie wykonano zmian");
                    }
               }

               Console.WriteLine($"{DateTime.Now}> Device Twin value was update.");
               Console.WriteLine();
 
            

              // await client.UpdateReportedPropertiesAsync(reportedProp);
          }

          private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
          {
             //  Console.WriteLine($"\t{DateTime.Now}> Device Twin. Desired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
             //  string nodeId = (string)userContext;
             // int newProdRate = desiredProperties["ProductionRate"];
              // string node = nodeId + "/ProductionRate";

              // OpcStatus result = client.WriteNode(node, newProdRate);
              // Console.WriteLine($"\t{DateTime.Now}> opcClient.WriteNode is result good: " + result.IsGood.ToString());
               TwinCollection reportedProperties = new TwinCollection();
               reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;
                

               await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
          }
          #endregion Device Twin




          public async Task InitializeHandlers(string userContext)
          {
               

             //  await client.SetMethodHandlerAsync("SendMessages", SendMessageToIOT, client);
               

               await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, client);
                ;
          }


     }
}
