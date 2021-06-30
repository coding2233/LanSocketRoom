using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace wanderer.lan
{
    public static class StringExtension
    {
        public static byte[] ToBytes(this string str)
        {
            return System.Text.Encoding.UTF8.GetBytes(str);
        }

        public static string ToLanString(this byte[] data)
        {
            string str = System.Text.Encoding.UTF8.GetString(data);
            //Debug.Log($"Bytes to string: {str}");
            return str;
        }

        public static IPEndPoint ToIPEndPoint(this string str)
        {
            string[] args = str.Split(':');
            IPAddress address = IPAddress.Parse(args[0]);
            int port = int.Parse(args[1]);
            return new IPEndPoint(address,port);
        }

    }
}
