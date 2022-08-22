// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//This code that messages to an IoT Hub for testing the routing as defined
//  in this article: https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing
//The scripts for creating the resources are included in the resources folder in this
//  Visual Studio solution. 

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace arm_read_write
{
    class Program
    {
        //This is the arm-read-write application that simulates a virtual device.
        //It writes messages to the IoT Hub, which routes the messages automatically to a storage account, 
        //where you can view them.

        //  This was derived by the (more complicated) tutorial for routing 
        //  https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing
       
        private static DeviceClient s_deviceClient;
        private static DeviceClient s2_deviceClient;
        private static DeviceClient s3_deviceClient;
        private static string s_myDeviceId;
        private static string s_iotHubUri;
        private static string s_deviceKey;

        private static string s2_myDeviceId;
        private static string s2_deviceKey;

        private static string s3_myDeviceId;
        private static string s3_deviceKey;

        private static async Task Main()
        {

           
            if (!ReadEnvironmentVariables())
            {
                Console.WriteLine();
                Console.WriteLine("Error! One or more environment variables not set");
                return;
            } 

            Console.WriteLine("write messages to a hub and use routing to write them to storage");
            ///Three separate devices
            ///Creates DeviceClient - Modified - created 3 seperate device clients 
            s_deviceClient = DeviceClient.Create(s_iotHubUri, 
              new DeviceAuthenticationWithRegistrySymmetricKey(s_myDeviceId, s_deviceKey), TransportType.Mqtt);

            s2_deviceClient = DeviceClient.Create(s_iotHubUri,
             new DeviceAuthenticationWithRegistrySymmetricKey(s2_myDeviceId, s2_deviceKey), TransportType.Mqtt);

            s3_deviceClient = DeviceClient.Create(s_iotHubUri,
             new DeviceAuthenticationWithRegistrySymmetricKey(s3_myDeviceId, s3_deviceKey), TransportType.Mqtt);

            //initiates a cancellation request 

            var cts = new CancellationTokenSource();
            //var cts2 = new CancellationTokenSource();

            /// var cts2 = new CancellationTokenSource();
            /// var cts3 = new CancellationTokenSource();
            /// passing token to SendDeviceToCloudMessagesAsync - allows us to have a token to be able to cancel 

            var messages = SendDeviceToCloudMessagesAsync(cts.Token, s_myDeviceId, s_deviceClient);
            var messages2 = SendDeviceToCloudMessagesAsync(cts.Token, s2_myDeviceId, s2_deviceClient);
            var messages3 = SendDeviceToCloudMessagesAsync(cts.Token, s3_myDeviceId, s3_deviceClient);

            Console.WriteLine("Press the Enter key to stop.");
            Console.ReadLine();

            /// cancels task - notification of cancellation 
            cts.Cancel();
            /// cts.Token throws an Obnject
            cts.Dispose();
            await messages;
            //await messages2;
            //await messages3;
        }

        /// <summary>
        /// Read local process environment variables for required values
        /// </summary>
        /// <returns>
        /// True if all required environment variables are set
        /// </returns>
        private static bool ReadEnvironmentVariables()
        {

            bool result = true;

            //Environment.SetEnvironmentVariable("simdev1-arm-app", s_myDeviceId);
            s_myDeviceId = "simdev1-arm-app";
            //s_myDeviceId = Environment.GetEnvironmentVariable("DEVICE_ID");
            //////if (s_myDeviceId is null)
            //////{
            //////    Console.WriteLine("s_myDevice");
            //////}
            s_iotHubUri = "iot-hub-mic-practice.azure-devices.net";
            //s_iotHubUri = Environment.GetEnvironmentVariable("IOT_HUB_URI");
            //////if (s_iotHubUri is null)
            //////{
            //////    Console.WriteLine("s_iotHubUri");
            //////} 
           s_deviceKey = "OASj9EVTf4k9dO4MV8cd1VGlX5n/Ryg8gooWnM2uX9M=";
            //s_deviceKey = Environment.GetEnvironmentVariable(" DEVICE_KEY");
            //////if (s_deviceKey is null)
            //////{
            //////    Console.WriteLine("s_deviceKey");
            //////}
            s2_myDeviceId = "simdev2-arm";
            //s2_myDeviceId = Environment.GetEnvironmentVariable("DEVICE_ID_2");
            //////if (s2_myDeviceId is null)
            //////{
            //////    Console.WriteLine("s2_myDevice");
            //////}
            s2_deviceKey = "rb1u+9fUoD+gqmpqjcjU0e1F6v0EP7F7n9HJ6TAfb6Q=";
            //s2_deviceKey = Environment.GetEnvironmentVariable("DEVICE_KEY_2");
            //////if (s2_deviceKey is null)
            //////{
            //////    Console.WriteLine("s2_deviceKey");
            //////}
            s3_myDeviceId = "simdev3-arm-app";
            //s3_myDeviceId = Environment.GetEnvironmentVariable("DEVICE_ID_3");
            //////if (s3_myDeviceId is null)
            //////{
            //////    Console.WriteLine("s3_myDeviceId");
            //////}
            s3_deviceKey = "A6goY7UjlGQXTRpY4PlZA557fOvMF439piXVXhznVYM=";
            //s3_deviceKey = Environment.GetEnvironmentVariable("DEVICE_KEY_3");
            //if (s3_deviceKey is null)
            //{
            //    Console.WriteLine("s3_deviceKey");

            //}


            if ((s_myDeviceId is null) || (s_iotHubUri is null) || (s_deviceKey is null) || (s2_myDeviceId is null) || (s2_deviceKey is null) || (s3_myDeviceId is null) || (s3_deviceKey is null))
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Send message to the Iot hub. This generates the object to be sent to the hub in the message.
        /// </summary>
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken token, string deviceID_generic, DeviceClient device_client_generic)
        {
            //string deviceID_class = deviceID_generic;
            //DeviceClient deviceClient = device_client_generic;
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();
            ///while the cancellation token method is  invoked
            while (!token.IsCancellationRequested)
            {

                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                string infoString;
                string levelValue;

                if (rand.NextDouble() > 0.7)
                {
                    if (rand.NextDouble() > 0.5)
                    {
                        levelValue = "critical";
                        infoString = "This is a critical message. ---------#";
                    }
                    else
                    {
                        levelValue = "storage";
                        infoString = "This is a storage message. --#";
                    }
                }
                else
                {
                    levelValue = "normal";
                    infoString = "This is a normal message. ||||||||||";
                }

                ///CReate new variable called telemetry Data Point (like array) putting 4 points into one variable 
                var telemetryDataPoint = new
                {
                    deviceId = deviceID_generic,
                    temperature = currentTemperature,
                    humidity = currentHumidity,
                    pointInfo = infoString
                };
                ///serialize the telemetry data and convert it to JSON.
                var telemetryDataString = JsonConvert.SerializeObject(telemetryDataPoint);

                /// Encode the serialized object using UTF-8 so it can be parsed by IoT Hub when
                /// processing messaging rules.
                using var message = new Message(Encoding.UTF8.GetBytes(telemetryDataString))
                {
                    ContentEncoding = "utf-8",
                    ContentType = "application/json",
                };

                ///Add one property to the message.
                message.Properties.Add("level", levelValue);


                /// Submit the message to the hub. - sending message to device client identified - MOdify? 
                /// loop through each client
                ///change to device_client 
                await device_client_generic.SendEventAsync(message);


                /// Print out the message. - useful for debugging - writes to terminal/VS
                Console.WriteLine("{0} > Sent message: {1}", DateTime.Now, telemetryDataString);

                await Task.Delay(1000);
            }
        }
    }
}
