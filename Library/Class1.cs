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
using Azure.Messaging.ServiceBus;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices;
using Microsoft.Rest;
using Message = Microsoft.Azure.Devices.Client.Message;





namespace Library
{
     public class Class1
     {
          private DeviceClient client;
          private OpcClient OPC;
          private RegistryManager registry;


          public Class1(DeviceClient deviceClient, OpcClient OPC , RegistryManager registry)
          {
               this.client = deviceClient;
               this.OPC = OPC;
               this.registry = registry;
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

               await UpdateTwinAsync(DeviceName, errorStatus, ProductionRate);
 
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
               // wartosci 
               var device_error_count = deviceError;




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
               //desired = oczekiwana
                
               // nazwa 
               var device_production = name + "_production_procent";
               // wartosci 
               var device_production_count = prodRate;

               if (desiredProp.Contains(device_production))
               {
                    
                    //nazwy do JSON
                    var device_name = name + "_production_procent";
                   
                         int production_rate;
                         if (int.TryParse((string)desiredProp[device_name], out production_rate))
                         {
                              OpcStatus status = OPC.WriteNode($"ns=2;s={deviceName}/ProductionRate", production_rate);
                         }

 


               }


               if (reportedProp.Contains(device_production))
               {
                    var productionInThisMoment = reportedProp[device_production];
                    // jak błąd jest inny
                    if (productionInThisMoment != device_production_count)
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
          public async Task ResetError(string deviceName)
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



          #region ServisBus - ERRORS 


          public async Task Processor_ProcessMessageAsync(ProcessMessageEventArgs arg)
          {
               Console.WriteLine($"RECEIVED MESSAGE:\n\t{arg.Message.Body}");
               var message = Encoding.UTF8.GetString(arg.Message.Body);
               ReadMessage mesg = JsonConvert.DeserializeObject<ReadMessage>(message);



               Console.WriteLine(mesg.windowEndTime);
               Console.WriteLine(mesg.DeviceName);
 
               string deviceId = mesg.DeviceName;

               Console.WriteLine("! ___________________ Zgłoszono wywyłanie metody EmergencyStop ");
               OPC.CallMethod($"ns=2;s={deviceId}", $"ns=2;s={deviceId}/EmergencyStop");
               Console.WriteLine("!__________________________________________________________");


               /*
               Console.WriteLine("!_________ Zgłoszono wywyłanie metody ProductionDecrease");

               OpcValue ProductionRate = client.ReadNode("ns=2;s=" + deviceId + "/ProductionRate");
               int production = (int)ProductionRate.Value;
               int low_production = production - 10;
               Console.WriteLine($" produkcja obecna {production} , zmniejszona {low_production} ");

               OpcStatus result = client.WriteNode("ns=2;s=" + deviceId + "/ProductionRate", low_production);


               Console.WriteLine("!__________________________________________________");
               */


               /* string methodName = "EmergencyStop";

                ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

                CloudToDeviceMethod method = new CloudToDeviceMethod(methodName);
                method.ResponseTimeout = TimeSpan.FromSeconds(30);
                CloudToDeviceMethodResult response = await serviceClient.InvokeDeviceMethodAsync(deviceId, method);
                Console.WriteLine($"Response status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
               */

          }

          public Task Processor_ProcessErrorAsync(ProcessErrorEventArgs arg)
          {
               Console.WriteLine(arg.Exception.ToString());

               return Task.CompletedTask;
          }
          #endregion


          #region ServisBus - PRODUCTION - ProductionDecrease


          public async Task Processor_ProcessMessageAsync2(ProcessMessageEventArgs arg)
          {
               Console.WriteLine("Podaj nazwe device  IOT hub : ");
               //string nazwaIOThub = Console.ReadLine() ?? string.Empty;
               string nazwaIOThub = "test_device";


               Console.WriteLine($"RECEIVED MESSAGE:\n\t{arg.Message.Body}");
               var message = Encoding.UTF8.GetString(arg.Message.Body);
               ReadMessage mesg = JsonConvert.DeserializeObject<ReadMessage>(message);


               Console.WriteLine(mesg.windowEndTime);
               Console.WriteLine(mesg.DeviceName);
               Console.WriteLine(mesg.productionDevice);



               Console.WriteLine("! ___________________ Zgłoszono wywyłanie metody ProductionDecrease ");
              
                

               string deviceId = mesg.DeviceName;


               await ChangeProduction(nazwaIOThub , deviceId);

               Console.WriteLine("!__________________________________________________________");


           

          }

          public Task Processor_ProcessErrorAsync2(ProcessErrorEventArgs arg)
          {
               Console.WriteLine(arg.Exception.ToString());

               return Task.CompletedTask;
          }
          #endregion


          #region Change ProductionRate 
          public async Task ChangeProduction(string nazwaIOThub , string deviceId)
          { 
               var twin = await registry.GetTwinAsync(nazwaIOThub);
               string json = JsonConvert.SerializeObject(twin, Formatting.Indented);
               JObject dana = JObject.Parse(json);

               var reportedProp = twin.Properties.Reported;
               var desiredProp = twin.Properties.Desired;         // desired - wartosc  oczekiwana na maszynie

               //object nameDevice = data.name; 
               var name = deviceId.Replace(" ", "");  // usuń spacje z nazwy 

               //nazwy do JSON
               var device_name = name + "_production_procent";
               // wartosci 
              
               string desired_value_procent = (string)dana["properties"]["desired"][device_name];
                
               // pobrać dane z reported dla devcice name którego dostałam w wiadomosci , dla jego production rate 
               string reported_value_procent = twin.Properties.Reported[device_name];
               if (desiredProp.Contains(device_name))
               {
                    // jeśli taki wpis istnieje to 
                    int desired_value = twin.Properties.Desired[device_name];
                    // wartosc obniz
                    int pom = desired_value - 10;
                    
                    // wyslac ponownie 
                    desiredProp[device_name] = pom;
                    // wywolanie by zrobiła się zmiana w Twin 
                    await registry.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);     // zaktualizujemy (wyślemy) w Azure
                    // zmiana na maszynie 
                    OpcStatus status = OPC.WriteNode($"ns=2;s={deviceId}/ProductionRate", pom);



               }
               else
               {
                    // jak nie bedzie nazwy device 
                    int reported_value_procent_new = twin.Properties.Reported[device_name];
                    int pom = reported_value_procent_new - 10;
                    
                    desiredProp[device_name] = pom;
                    await registry.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);     // zaktualizujemy (wyślemy) w Azure
                    OpcStatus status = OPC.WriteNode($"ns=2;s={deviceId}/ProductionRate", pom);

               }
 

          }


          #endregion



          #region ReadMessage - region 
          public class ReadMessage
          {
               public DateTime windowEndTime { get; set; }
               public string DeviceName { get; set; }

               public string productionDevice { get; set; }

          }

          #endregion

     }
}
