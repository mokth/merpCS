﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Bluetooth;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;
using SQLite;
using System.Threading;
using Java.Util;

namespace wincom.mobile.erp
{
	[Activity (Label = "INVOICE LIST")]			
	public class InvoiceAllActivity : Activity,IEventListener
	{
		ListView listView ;
		List<Invoice> listData = new List<Invoice> ();
		string pathToDatabase;
		string compCode;
		string branchCode;
		//BluetoothAdapter mBluetoothAdapter;
		BluetoothSocket mmSocket;
		BluetoothDevice mmDevice;
		//Thread workerThread;
		//Stream mmOutputStream;
		AdPara apara=null;
		CompanyInfo compinfo;
		DateTime sdate;
		DateTime edate;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			EventManagerFacade.Instance.GetEventManager().AddListener(this);
			// Create your application here
			SetContentView (Resource.Layout.ListView);
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;
			Utility.GetDateRange (ref sdate,ref edate);
			populate (listData);
			apara =  DataHelper.GetAdPara (pathToDatabase,compCode,branchCode);
			listView = FindViewById<ListView> (Resource.Id.feedList);
//			TableLayout tlay = FindViewById<TableLayout> (Resource.Id.tableLayout1);
//			tlay.Visibility = ViewStates.Invisible;
			Button butNew= FindViewById<Button> (Resource.Id.butnewInv); 
			butNew.Visibility = ViewStates.Invisible;
			Button butInvBack= FindViewById<Button> (Resource.Id.butInvBack); 
			butInvBack.Click+= (object sender, EventArgs e) => {
				StartActivity(typeof(TransListActivity));
			};

			listView.ItemClick += OnListItemClick;
			listView.ItemLongClick += OnListItemLongClick;
			//listView.Adapter = new CusotmListAdapter(this, listData);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<Invoice> (this, listData, Resource.Layout.ListItemRow, viewdlg);

		}

		private void SetViewDelegate(View view,object clsobj)
		{
			Invoice item = (Invoice)clsobj;
			view.FindViewById<TextView> (Resource.Id.invdate).Text = item.invdate.ToString ("dd-MM-yy");
			view.FindViewById<TextView> (Resource.Id.invno).Text = item.invno;
			view.FindViewById<TextView> (Resource.Id.trxtype).Text = item.trxtype;
			view.FindViewById<TextView>(Resource.Id.invcust).Text = item.description;
			//view.FindViewById<TextView> (Resource.Id.Amount).Text = item.amount.ToString("n2");
			view.FindViewById<TextView> (Resource.Id.TaxAmount).Text = item.taxamt.ToString("n2");
			double ttl = item.amount + item.taxamt;
			view.FindViewById<TextView> (Resource.Id.TtlAmount).Text =ttl.ToString("n2");
			ImageView img = view.FindViewById<ImageView> (Resource.Id.printed);
			if (!item.isPrinted)
				img.Visibility = ViewStates.Invisible;
		}

		protected override void OnResume()
		{
			base.OnResume();
			listData = new List<Invoice> ();
			populate (listData);
			apara =  DataHelper.GetAdPara (pathToDatabase,compCode,branchCode);
			listView = FindViewById<ListView> (Resource.Id.feedList);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<Invoice> (this, listData, Resource.Layout.ListItemRow, viewdlg);
		}

		void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e) {
			Invoice item = listData.ElementAt (e.Position);
			var intent = new Intent(this, typeof(InvItemHisActivity));
			intent.PutExtra ("invoiceno",item.invno );
			intent.PutExtra ("custcode",item.custcode );
			StartActivity(intent);
		}

		void OnListItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e) {
			Invoice item = listData.ElementAt (e.Position);
			PopupMenu menu = new PopupMenu (e.Parent.Context, e.View);
			menu.Inflate (Resource.Menu.popupHis);
			menu.MenuItemClick += (s1, arg1) => {
				
				if (arg1.Item.TitleFormatted.ToString ().ToLower () == "print") {
					PrintInv (item,1);	
				}else if (arg1.Item.TitleFormatted.ToString ().ToLower () == "print 2 copy") {
					PrintInv (item,2);	
				}  else if (arg1.Item.TitleFormatted.ToString ().ToLower () == "filter") {
					ShowDateRangeLookUp();
				} 
			};
			menu.Show ();
		}

		void ShowDateRangeLookUp()
		{
			var intent = new Intent (this, typeof(DateRange));
			intent.PutExtra ("eventid", "2020");
			StartActivity (intent);
		}

		void populate(List<Invoice> list)
		{
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list2 = db.Table<Invoice>()
					.Where(x=>x.isUploaded==true
						&& x.CompCode == compCode && x.BranchCode == branchCode
						 &&x.invdate>=sdate&&x.invdate<=edate)
					.OrderByDescending (x => x.invno)
					.ToList<Invoice>();
				foreach(var item in list2)
				{
					list.Add(item);
				}

			}
			compinfo = DataHelper.GetCompany (pathToDatabase,compCode,branchCode);
		}


		void PrintInv(Invoice inv,int noofcopy)
		{
			Toast.MakeText (this, "print....", ToastLength.Long).Show ();	
			InvoiceDtls[] list;
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)){
				var ls= db.Table<InvoiceDtls> ().Where (x => x.invno==inv.invno&&x.CompCode==inv.CompCode&&x.BranchCode==inv.BranchCode).ToList<InvoiceDtls>();
				list = new InvoiceDtls[ls.Count];
				ls.CopyTo (list);
			}
			mmDevice = null;
			findBTPrinter ();
			if (mmDevice != null) {
				StartPrint (inv, list,noofcopy);
			}
		}

		void StartPrint(Invoice inv,InvoiceDtls[] list,int noofcopy )
		{
			string userid = ((GlobalvarsApp)this.Application).USERID_CODE;
			PrintInvHelper prnHelp = new PrintInvHelper (pathToDatabase, userid,compCode,branchCode);
			string msg =prnHelp.OpenBTAndPrint (mmSocket, mmDevice, inv, list,noofcopy);
			Toast.MakeText (this, msg, ToastLength.Long).Show ();	
		}

		void findBTPrinter(){
			string printername = apara.PrinterName.Trim ().ToUpper ();
			Utility util = new Utility ();
			string msg = "";
			mmDevice = util.FindBTPrinter (printername,ref  msg);
			Toast.MakeText (this, msg, ToastLength.Long).Show ();	
		}

		public event nsEventHandler eventHandler;

		public void FireEvent(object sender,EventParam eventArgs)
		{
			if (eventHandler != null)
				eventHandler (sender, eventArgs);
		}

		public void PerformEvent(object sender, EventParam e)
		{
			switch (e.EventID) {
			case 2020:
				sdate =Utility.ConvertToDate(e.Param ["DATE1"].ToString ());
				edate=Utility.ConvertToDate(e.Param ["DATE2"].ToString ());
				break;
			}
		}
	}
}

