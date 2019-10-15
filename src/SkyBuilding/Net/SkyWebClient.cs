﻿using System;
using System.Net;

namespace SkyBuilding.Net
{
    /// <summary>
    /// Web客户端
    /// </summary>
    public class SkyWebClient : WebClient
    {
        /// <summary>
        /// 静态构造函数
        /// </summary>
        static SkyWebClient()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        }

        /// <summary>
        /// 过期时间（单位：毫秒）
        /// </summary>
        public int Timeout { get; set; } = 5000;

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);

            request.Timeout = Timeout;

            return request;
        }
    }
}
