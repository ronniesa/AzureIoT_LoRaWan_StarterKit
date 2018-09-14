//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LoRaWan.NetworkServer
{


    class LoraDecoders
    {
        public static string DecodeMessage(byte[] payload, uint fport, string SensorDecoder)
        {
            Type decoderType = typeof(LoraDecoders);
            MethodInfo toInvoke = decoderType.GetMethod(
               SensorDecoder, BindingFlags.Static | BindingFlags.NonPublic);

            if (toInvoke != null)
            {
       
                return (string)toInvoke.Invoke(null, new object[] { payload, fport});
            }
            else
            {
                var base64Payload = Convert.ToBase64String(payload);
                return $"{{\"error\": \"No '{SensorDecoder}' decoder found\", \"rawpayload\": \"{base64Payload}\"}}";
            }
        }
               
        private static string DecoderGpsSensor(byte[] payload, uint fport)
        {
            var result = Encoding.ASCII.GetString(payload);
            string[] values = result.Split(':');
            return String.Format("{{\"latitude\": {0} , \"longitude\": {1}}}", values[0], values[1]);
        }
        
        private static string DecoderTemperatureSensor(byte[] payload, uint fport)
        {
            var result = Encoding.ASCII.GetString(payload);
            return String.Format("{{\"temperature\": {0}}}", result);
        }

        private static string DecoderValueSensor(byte[] payload, uint fport)
        {
            var result = Encoding.ASCII.GetString(payload);
            return String.Format("{{\"value\": {0}}}", result);

        }

        private static string DecoderMoteIISensor(byte[] bytes, uint port)
        {

            string base64 = "ACMPCi4ncABCLJoGITYEpA==";

            bytes = Convert.FromBase64String(base64);


            dynamic decoded = new JObject();

            //default lora mote
            if (port == 2)
            {
                decoded.led = bytes[0];
                decoded.pressure = (bytes[1] * 256F + bytes[2]) / 10F;
                decoded.temperature = (bytes[3] * 256F + bytes[4]) / 100F;
                decoded.altitudeBar = (bytes[5] * 256F + bytes[6]) / 10F;
                if (bytes[7] == 0)
                {
                    decoded.powerState = "USB";
                }
                else if (bytes[7] == 0xFF)
                {
                    decoded.powerState = "Error";
                }
                else
                {
                    decoded.powerState = "battery";
                    decoded.battery = bytes[7] * 100F / 254F;
                }

                //// manually extract 24-bit latitude
                int lat_val = bytes[8] << 16 | bytes[9] << 8 | bytes[10];
                lat_val = !Convert.ToBoolean(lat_val & 0x800000) ? lat_val : ((0xffffff - lat_val + 1) * -1);

                //// manually extract 24-bit longitude
                int lng_val = bytes[11] << 16 | bytes[12] << 8 | bytes[13];
                lng_val = !Convert.ToBoolean(lng_val & 0x800000) ? lng_val : ((0xffffff - lng_val + 1) * -1);



                decoded.lat = (float)lat_val / Math.Pow(2, 23) * 90;
                decoded.lng = (float)lng_val / Math.Pow(2, 23) * 180;
                decoded.altitudeGps = bytes[14] * 256 + bytes[15];

                return decoded.ToString(Newtonsoft.Json.Formatting.None);
            }

            return null;
        }

    }

}
