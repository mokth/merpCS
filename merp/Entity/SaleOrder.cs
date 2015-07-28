﻿using System;
using SQLite;

namespace wincom.mobile.erp
{
	public class SaleOrder
	{
		[PrimaryKey] 
		public string sono  { get; set; }
		public string custpono  { get; set; }
		public string trxtype  { get; set; }
		public DateTime sodate  { get; set; }
		public DateTime created  { get; set; }
		public double amount { get; set; }
		public double taxamt { get; set; }
		public string custcode { get; set; }
		public string description { get; set; }
		public DateTime uploaded  { get; set; }
		public bool isUploaded  { get; set; }
		public bool isPrinted  { get; set; }
		public string remark { get; set; }
		public string CompCode{ get; set;}
		public string BranchCode{ get; set;}
	}

	public class SaleOrderDtls
	{
		[PrimaryKey, AutoIncrement]
		public int ID { get; set; }
		public string sono  { get; set; }
		public double amount { get; set; }
		public double netamount { get; set; }
		public string icode { get; set; }
		public string description { get; set; }
		public double qty { get; set; }
		public double price { get; set; }
		public string taxgrp { get; set; }
		public double tax { get; set; }
		public bool isincludesive { get; set; }
		public string remark { get; set; }
		public string CompCode{ get; set;}
		public string BranchCode{ get; set;}
	}
}

