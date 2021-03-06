﻿using System;
using System.ServiceModel;

namespace wincom.mobile.erp
{
	public class WCFHelper
	{
		public readonly EndpointAddress EndPoint;
		private Service1Client _client;

		public WCFHelper()
		{
		  //EndPoint = new EndpointAddress("http://www.wincomcloud.com/Wfc/Service1.svc");
			EndPoint = new EndpointAddress("http://www.wincomcloud.com/WfcCS/Service1.svc");
			//EndPoint = new EndpointAddress("http://www.wincomcloud.com/WfcV2/Service1.svc");

		}

		public static string GetDownloadDBUrl()
		{
			return "http://www.wincomcloud.com/wfcCS/dbfiles/";
		}

		public static string GeUploadDBUrl()
		{
			return "http://www.wincomcloud.com/UploadDb/uploadDb.aspx";
		}

		public  Service1Client GetServiceClient()
		{
			InitializeServiceClient ();

			return _client;
		}

		private void InitializeServiceClient()
		{
			BasicHttpBinding binding = CreateBasicHttp();

			_client = new Service1Client(binding, EndPoint);

		}

		private static BasicHttpBinding CreateBasicHttp()
		{
			BasicHttpBinding binding = new BasicHttpBinding
			{
				Name = "basicHttpBinding",
				MaxBufferSize = 2147483647,
				MaxReceivedMessageSize = 2147483647
			};
			TimeSpan timeout = new TimeSpan(0, 0, 30);
			binding.SendTimeout = timeout;
			binding.OpenTimeout = timeout;
			binding.ReceiveTimeout = timeout;
			return binding;
		}
	}
}

