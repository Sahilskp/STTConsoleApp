using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;


namespace SpeechText
{
    class Program
    {
        // ADD THIS PART TO YOUR CODE

        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = "https://askpdb.documents.azure.com:443/";
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "mCIqMQe9CCsqXNrFoJoEYYGy5J5QGIwV7RXb0UcqUNHeur0NWOhnxMEgXgw8enud6eYiLRnGb4jcsLoi5yGskw==";

        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database ;

        // The container we will create.
        private Container container;

         // The name of the database and container we will create
        private string databaseId = "AudioTXT";
        private string containerId = "SpeechToText";

        public static String text = "";

        // public static async Task RecognizeSpeechAsync()
        // {
        //     var config = SpeechConfig.FromSubscription("4da669a121a445dcab68a3b2facd7527", "eastus");
        //     config.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "5000");

        //     using(var audioInput = AudioConfig.FromWavFileInput("anikka2.wav"))
        //     {
        //         using(var recognizer = new SpeechRecognizer(config))
        //         {
        //             Console.WriteLine("Say something...");

        //             var result = await recognizer.RecognizeOnceAsync();

        //             if(result.Reason == ResultReason.RecognizedSpeech)
        //             {
        //                 Console.WriteLine($"Text Recognized  : {result.Text}");

        //             }
        //             else if(result.Reason == ResultReason.NoMatch)
        //             {
        //                 Console.WriteLine("No speech recognized");    
        //             }
        //             else if(result.Reason == ResultReason.Canceled)
        //             {
        //                 var cancellationDetails = CancellationDetails.FromResult(result);
        //                 Console.WriteLine($"Speech recognition cancelled : {cancellationDetails.Reason}");    

        //                 if(cancellationDetails.Reason == CancellationReason.Error)
        //                 {
        //                     Console.WriteLine($"ErrorCode {cancellationDetails.ErrorCode}  ");
        //                     Console.WriteLine($"ErrorDetails {cancellationDetails.ErrorDetails}  ");
                            
        //                 }
        //             }
        //         }
        //     }
                
        // }



        public static async Task SpeechContinuousRecognitionAsync()
            {
                // Creates an instance of a speech config with specified subscription key and service region.
                // Replace with your own subscription key and service region (e.g., "westus").
                var config = SpeechConfig.FromSubscription("4da669a121a445dcab68a3b2facd7527", "eastus");
                config.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "10000");
                //config.SetProperty("Conversation_Initial_Silence_Timeout", "10000");
                using (var audioInput = AudioConfig.FromWavFileInput("anikka2.wav"))
                {
                // Creates a speech recognizer from microphone.
                using (var recognizer = new SpeechRecognizer(config))
                    {
                         
                        // Subscribes to events.
                        recognizer.Recognizing += (s, e) => {
                            //Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                        };

                        recognizer.Recognized += (s, e) => {
                            var result = e.Result;
                            Console.WriteLine($"Reason: {result.Reason.ToString()}");
                            if (result.Reason == ResultReason.RecognizedSpeech)
                            {
                                    //Console.WriteLine($"Final result: Text: {result.Text}.");
                                    text += result.Text;
                                   
                            }
                        };

                        recognizer.Canceled += (s, e) => {
                            Console.WriteLine($"\n    Recognition Canceled. Reason: {e.Reason.ToString()}, CanceledReason: {e.Reason}");
                        };

                        recognizer.SessionStarted += (s, e) => {
                            Console.WriteLine("\n    Session started event.");
                        };

                        recognizer.SessionStopped += (s, e) => {
                            Console.WriteLine("\n    Session stopped event.");
                            // try
                            // {
                            //     FileWriter(text);

                            // }
                            // catch(Exception exp)
                            // {
                            //     Console.WriteLine(exp.Message);
                            // }
                            
                        };

                        // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                         
                        do
                        {
                            
                            Console.WriteLine("Press Enter to stop");
                        } while (Console.ReadKey().Key != ConsoleKey.Enter);

                        // Stops recognition.
                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    }
                }
            }
        

        

        static void FileWriter(String txt)
        {
            string datetimeString = string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}",DateTime.Now);
            Console.WriteLine(datetimeString);
            StreamWriter sw = new StreamWriter($"audioText{datetimeString}.txt");
            sw.WriteLine(txt);
            sw.Close();
        }
        static async Task Main(string[] args)
        {
            SpeechContinuousRecognitionAsync().Wait();
            try
            {
                Console.WriteLine("Beginning operations...\n");
                Program p = new Program();
                await p.GetStartedDemoAsync();

            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
            
        }

        public async Task GetStartedDemoAsync()
        {
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            await this.CreateDatabaseAsync();
            await this.CreateContainerAsync();

            await this.AddItemsToContainerAsync();
        }

        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }

        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.container = await this.database.CreateContainerIfNotExistsAsync(containerId, "/batchId");
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }
        private async Task AddItemsToContainerAsync()
        {
            Speech audiotxt = new Speech
            {
                Id = string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}.txt",DateTime.Now),
                batchId = text
            };
            ItemResponse<Speech> SpeechResponse = await this.container.CreateItemAsync<Speech>(audiotxt, new PartitionKey(audiotxt.batchId));
        }
    }

    public class Speech
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string batchId { get; set; }
    }
}
