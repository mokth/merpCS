using System;
using System.Linq;
using WcfServiceItem;
using System.Collections.Generic;
using Android.App;
using SQLite;
using Android.Widget;

namespace wincom.mobile.erp
{
	public delegate void OnUploadDoneDlg(Activity activity, int count,string msg);

	public class UploadHelper:Activity
	{
		Service1Client _client;
		WCFHelper _wfc = new WCFHelper();
		volatile List<OutLetBillEx> bills = new List<OutLetBillEx> ();
		volatile string _errmsg;
		volatile int invcount =0;
		public OnUploadDoneDlg Uploadhandle;
		public Activity CallingActivity=null;
		string comp;
		string brn;
		string pathToDatabase;

		public void startUpload()
		{
			comp =((GlobalvarsApp)CallingActivity.Application).COMPANY_CODE;
			brn =((GlobalvarsApp)CallingActivity.Application).BRANCH_CODE;
			pathToDatabase = ((GlobalvarsApp)CallingActivity.Application).DATABASE_PATH;
			invcount =0;
			_errmsg = "";
			_client = _wfc.GetServiceClient ();	
			_client.UploadOutletBillsExCompleted+= ClientOnUploadOutletBillsCompleted;
			UploadBillsToServer ();
		}

		private void UploadBillsToServer()
		{
			PhoneTool ptool = new PhoneTool ();
			string phone = ptool.PhoneNumber ();
			string serial =ptool.DeviceIdIMEI();
		
			bills = GetBills();
//			if (bills.Count == 0)
//				bills = GetCNs();	
			invcount += bills.Count;
			if (bills.Count > 0) {
				_client.UploadOutletBillsExAsync (bills.ToArray (), comp, brn, serial, phone);
			} else {
				RunOnUiThread (() => Uploadhandle.Invoke(CallingActivity,invcount,_errmsg));
			}
		}

		public void UpdateUploadStat()
		{
			DateTime now = DateTime.Now;
			try {
				using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
					var list1 = db.Table<Invoice> ().Where (x => x.isUploaded == false&&x.CompCode==comp&&x.BranchCode==brn).ToList<Invoice> ();
					List<Invoice> invlist = new List<Invoice>();
					foreach (OutLetBillEx bill in bills) {
						var found = list1.Where(x=>x.invno==bill.InvNo && bill.TrxType==x.trxtype).ToList<Invoice>();
						if (found.Count>0)
						{  
							found[0].isUploaded = true;
							found[0].uploaded = now;
							invlist.Add(found[0]);
						}

					}	

					if (invlist.Count>0)
						db.UpdateAll (invlist);

					UploadBillsToServer ();
				}
			} catch (Exception ex) {
				_errmsg = "Update status Error." + ex.Message;
			}
		}

		public List<OutLetBillEx> GetBills()
		{
			string userid =((GlobalvarsApp)CallingActivity.Application).USERID_CODE;

			bills = new List<OutLetBillEx> ();
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var list1 = db.Table<Invoice> ().Where(x=>x.isUploaded==false &&x.CompCode==comp&&x.BranchCode==brn)
					.OrderBy(x=>x.invno)
					.Take(10)
					.ToList<Invoice> ();
				
				var list2 = db.Table<InvoiceDtls>().Where(x=>x.CompCode==comp&&x.BranchCode==brn).ToList<InvoiceDtls> ();

				bool isFirstItem = false;
				double ttlAmt = 0;
				foreach (Invoice inv in list1) {
					isFirstItem = true;
				 	Trader trd =DataHelper.GetTrader (pathToDatabase, inv.custcode, comp, brn);
					var list3 = list2.Where (x => x.invno == inv.invno).ToList<InvoiceDtls> ();
					ttlAmt = 0;
					foreach (InvoiceDtls invdtl in list3) {
						ttlAmt = ttlAmt + invdtl.netamount + invdtl.tax;
					}
					double monthIns = Math.Round (ttlAmt / inv.InstMonth, 2);
					foreach (InvoiceDtls invdtl in list3) {
						OutLetBillEx bill = new OutLetBillEx ();
						bill.UserID = userid;
						bill.BranchCode = inv.BranchCode;
						bill.CompanyCode = inv.CompCode;
						bill.Created = inv.created;
						bill.CustCode = inv.custcode;
						bill.ICode = invdtl.icode;
						bill.InvDate = inv.invdate;
						bill.InvNo = inv.invno;
						bill.IsInclusive = invdtl.isincludesive;
						bill.Amount = invdtl.amount;
						bill.NetAmount = invdtl.netamount;
						bill.TaxAmt = invdtl.tax;
						bill.TaxGrp = invdtl.taxgrp;
						bill.UPrice = invdtl.price;
						bill.Qty = invdtl.qty;
						bill.TrxType = inv.trxtype;
						bill.CCNo = inv.CCardNo+"|"+inv.CCBank;
						bill.Remark = monthIns.ToString("n2");
						bill.InsMonth = inv.InstMonth.ToString();
						if (isFirstItem) {
							isFirstItem = false;
							bill.Addr1 = trd.Addr1;
							bill.Addr2 = trd.Addr2;
							bill.Addr3 = trd.Addr3;
							bill.Addr4 = trd.Addr4;
							bill.Name = trd.CustName;
							bill.NRIC = trd.NRIC;
							bill.Tel = trd.Tel;
						}

						bills.Add (bill);
					}
				}
			}

			return bills;
		}


		public void ClientOnUploadOutletBillsCompleted(object sender, UploadOutletBillsExCompletedEventArgs e)
		{
			
			if ( e.Error != null)
			{
				_errmsg =  e.Error.Message;
			
			}
			else if ( e.Cancelled)
			{
				_errmsg = "Request was cancelled.";
			}
			else
			{
				_errmsg = e.Result.ToString ();
				if (_errmsg== "OK") {
					UpdateUploadStat();
				}
			}

			//RunOnUiThread (() => DownloadCOmpleted (msg));

		}

//		void DownloadCOmpleted (string msg)
//		{
//			Toast.MakeText (this, msg, ToastLength.Long).Show ();	
//
//		}
	}
}

