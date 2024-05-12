using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Net.Mime;
using System.Text;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Exceptions;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Amqp.Framing;
using Opc.Ua;


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
          public async Task SendTelemetry(string DeviceName, object WorkorderId, object ProductionStatus, object Temperature, object ProductionRate,
                        object GoodCount, object BadCount, object DeviceErrors)
          {
               var twin = await client.GetTwinAsync();
               var reportedProperties = twin.Properties.Reported;
               var nameDevice = DeviceName.Replace(" ", "");
               var device_error = nameDevice + "_numer_bledu";
               var errorStatus = DeviceErrors;
               bool DataNoChange = false;
               //DeviceError wysylamy tylko gdy sie zmini 

               if (reportedProperties.Contains(device_error))
               {
                    var currentError = reportedProperties[device_error];
                    DataNoChange = (currentError == errorStatus);
               }
               if (DataNoChange)
               {
                    // błąd się nei zmienił wiec nie wysyłamy wartosci error
                    var selectedData = new
                    {
                         DeviceName = DeviceName,
                         WorkorderId = WorkorderId,
                         ProductionStatus = ProductionStatus,
                         Temperature = Temperature,
                         ProductionRate = ProductionRate,
                         GoodCount = GoodCount,
                         BadCount = BadCount,

                    };
                    await SendMessageToIOT(selectedData);
                    Console.WriteLine(selectedData);
               }
               else
               {
                    var selectedData = new
                    {
                         DeviceName = DeviceName,
                         WorkorderId = WorkorderId,
                         ProductionStatus = ProductionStatus,
                         Temperature = Temperature,
                         ProductionRate = ProductionRate,
                         GoodCount = GoodCount,
                         BadCount = BadCount,
                         DeviceErrors = DeviceErrors,
                         // W przypadku zmiany wartść należy wysłać pojedyńczy komunikat D2C do platformy iOT ( punkt 2.7) 
                    };
                    Console.WriteLine(selectedData);
                    await SendMessageToIOT(selectedData);
               }

               await UpdateTwinAsync(nameDevice, errorStatus, ProductionRate);


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
          public async Task UpdateTwinAsync(string deviceName, object deviceError, object prodRate)
          { 
               //DeviceError wysylamy tylko gdy sie zmini 
               // bliżniak jak chcemy zmienić jakąś konfiguracje  lub gdy urządzenie reportuje że zmienilo konfiguracje 
               var twin = await client.GetTwinAsync();

               var reportedProp = twin.Properties.Reported;       //reportet - wartosc na  maszynie
               var desiredProp = twin.Properties.Desired;         // desired - wartosc  oczekiwana na maszynie

               //object nameDevice = data.name; 
               var name = deviceName.Replace(" ", "");  // usuń spacje z nazwy 

               //nazwy do JSON
               var device_error = name + "_numer_bledu";
               var device_production = name + "_production_procent";
               // wartosci 
               var device_error_count = deviceError;
               var device_production_count = prodRate;
               


               // OBSŁUGA ZMAINY BLEDU 
               // Jeśli już taki wpis istnieje
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
                        // Console.WriteLine(" Brak zmiany bledu - nie wykonano zmian");
                    }
               }
               else
               {
                    // jeśli nie ma takiego wpisu dla Device w reported to dodaj
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

               // OBSŁUGA ZMAINY % PRODUKCJI  
               // Jeśli już taki wpis istnieje
               if (reportedProp.Contains(device_production))
               {
                    var errorInTgisMoment = reportedProp[device_production];
                    // jak błąd jest inny
                    if (errorInTgisMoment != device_production_count)
                    {

                         // zmienił się błąd , trzeba wyświeylić informacje
                         var updateProp = new TwinCollection();
                         updateProp[device_production] = device_production_count;
                         try
                         {
                              await client.UpdateReportedPropertiesAsync(updateProp);
                              Console.WriteLine("Zaktualowano % produkcji dla :   ", device_production, ".");
                              Console.WriteLine($"{DateTime.Now}> Device Twin   was update.");
                         }
                         catch (IotHubException ex)
                         {
                              Console.WriteLine("Blad podczas zmiany wartosci bledu", device_production);
                         }
                    }
                    else
                    {
                         //Console.WriteLine(" Brak zmiany bledu - nie wykonano zmian");
                    }
               }
               else
               {
                    // jeśli nie ma takiego wpisu dla Device w reported to dodaj
                    var updateProp = new TwinCollection();
                    updateProp[device_production] = device_production_count;
                    try
                    {
                         await client.UpdateReportedPropertiesAsync(updateProp);
                         Console.WriteLine("Zaktualowano % produkcji dla :    ", device_production, ".");
                         Console.WriteLine($"{DateTime.Now}> Device Twin   was update.");
                    }
                    catch (IotHubException ex)
                    {
                         Console.WriteLine("Blad podczas zmiany wartosci bledu", device_production);
                    }
               }


               Console.WriteLine();
          }




          private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
          {
                 Console.WriteLine($"\t{DateTime.Now}> Device Twin. Desired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
              
               TwinCollection reportedProperties = new TwinCollection();
               reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;

               await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
          }
          #endregion Device Twin


          #region Direct Methods - ResetErrorStatus
            public   async Task ResetError(string deviceName)
            {
                 Console.WriteLine($"\tMETHOD EXECUTED ResetErrorStatus FROM : {deviceName}");
                 OPC.CallMethod($"ns=2;s={deviceName}", $"ns=2;s={deviceName}/ResetErrorStatus");
                 await Task.Delay(1000); 
            }

          private async Task<MethodResponse> ResetErrorStatus(MethodRequest methodRequest, object userContext)
          {
               var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new {deviceName = default(string)});
               Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name} na {payload.deviceName}"); 
               await ResetError(payload.deviceName);
               return new MethodResponse(0);
          }
          #endregion



          #region Direct Methods - Emergency Stop
          public async Task EmergencyStopStatus(string deviceName)
          {
               Console.WriteLine($"\tMETHOD EXECUTED Emergency Stop FROM : {deviceName}");
               OPC.CallMethod($"ns=2;s={deviceName}", $"ns=2;s={deviceName}/EmergencyStop");
               await Task.Delay(1000);
          }

          private async Task<MethodResponse> EmergencyStop(MethodRequest methodRequest, object userContext)
          {
               var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { deviceName = default(string) });
               Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name} na {payload.deviceName}");
               await EmergencyStopStatus(payload.deviceName);
               return new MethodResponse(0);
          }

          #endregion



          #region InitializeHandlers
          public async Task InitializeHandlers()
          {
                
               await client.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatus, client);
               await client.SetMethodHandlerAsync("EmergencyStop", EmergencyStop, client);


          }

          #endregion

     }
}
