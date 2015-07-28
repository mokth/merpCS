using System;
using System.Linq;
using WcfServiceItem;
using System.Collections.Generic;
using Android.App;
using SQLite;
using Android.Widget;

namespace wincom.mobile.erp
{
	
	public class UploadCNHelper:Activity
	{
		Service1Client _client;
		WCFHelper _wfc = new WCFHelper();
		volatile List<OutLetBill> bills = new List<OutLetBill> ();
		volatile string _errmsg;
		volatile int invcount =0;
		public OnUploadDoneDlg Uploadhandle;
		public Activity CallingActivity=null;

		public void startUpload()
		{
			invcount =0;
			_errmsg = "";
			_client = _wfc.GetServiceClient ();	
			_client.UploadOutletBillsCompleted+= ClientOnUploadOutletBillsCompleted;
			UploadBillsToServer ();
		}

		private void UploadBillsToServer()
		{
			PhoneTool ptool = new PhoneTool ();
			string phone = ptool.PhoneNumber ();
			string serial =ptool.DeviceIdIMEI();
			string comp =((GlobalvarsApp)CallingActivity.Application).COMPANY_CODE;
			string brn =((GlobalvarsApp)CallingActivity.Application).BRANCH_CODE;
			bills = GetCNs();	
			invcount += bills.Count;
			if (bills.Count > 0) {
				_client.UploadOutletBillsAsync (bills.ToArray (), comp, brn, serial, phone);
			} else {
				RunOnUiThread (() => Uploadhandle.Invoke(CallingActivity,invcount,_errmsg));
			}
		}

		public void UpdateUploadStat()
		{
			string pathToDatabase = ((GlobalvarsApp)CallingActivity.Application).DATABASE_PATH;
			DateTime now = DateTime.Now;
			try {
				using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
					
					var list2 = db.Table<CNNote> ().ToList<CNNote> ().Where (x => x.isUploaded == false).ToList<CNNote> ();
					List<CNNote> invlist2 = new List<CNNote>();
					foreach (OutLetBill bill in bills) {
						var found = list2.Where(x=>x.cnno==bill.InvNo && bill.TrxType==x.trxtype).ToList<CNNote>();
						if (found.Count>0)
						{  
							found[0].isUploaded = true;
							found[0].uploaded = now;
							invlist2.Add(found[0]);
						}

					}	

					if (invlist2.Count>0)
						db.UpdateAll (invlist2);
					UploadBillsToServer ();
				}
			} catch (Exception ex) {
				_errmsg = "Update status Error." + ex.Message;
			}
		}

		public List<OutLetBill> GetCNs()
		{
			string pathToDatabase = ((GlobalvarsApp)CallingActivity.Application).DATABASE_PATH;
			string comp =((GlobalvarsApp)CallingActivity.Application).COMPANY_CODE;
			string brn =((GlobalvarsApp)CallingActivity.Application).BRANCH_CODE;
			string userid =((GlobalvarsApp)CallingActivity.Application).USERID_CODE;

			bills = new List<OutLetBill> ();
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var list1 = db.Table<CNNote> ().Where(x=>x.isUploaded==false)
					.OrderBy(x=>x.cnno)
					.Take(10)
					.ToList<CNNote> ();

				var list2 = db.Table<CNNoteDtls> ().ToList<CNNoteDtls> ();

				foreach (CNNote inv in list1) {
					var list3 = list2.Where (x => x.cnno == inv.cnno).ToList<CNNoteDtls> ();
					foreach (CNNoteDtls invdtl in list3) {
						OutLetBill bill = new OutLetBill ();
						bill.UserID = userid;
						bill.BranchCode = brn;
						bill.CompanyCode = comp;
						bill.Created = inv.created;
						bill.CustCode = inv.custcode;
						bill.ICode = invdtl.icode;
						bill.InvDate = inv.invdate;
						bill.InvNo = inv.cnno;
						bill.CNInvNo = inv.invno;
						bill.IsInclusive = invdtl.isincludesive;
						bill.Amount = invdtl.amount;
						bill.NetAmount = invdtl.netamount;
						bill.TaxAmt = invdtl.tax;
						bill.TaxGrp = invdtl.taxgrp;
						bill.UPrice = invdtl.price;
						bill.Qty = invdtl.qty;
						bill.TrxType = inv.trxtype;
						bills.Add (bill);
					}
				}
			}

			return bills;
		}

		public void ClientOnUploadOutletBillsCompleted(object sender, UploadOutletBillsCompletedEventArgs e)
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

