﻿
using System;
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
	[Activity (Label = "SALES ORDER")]			
	public class SalesOrderActivity : Activity
	{
		ListView listView ;
		List<SaleOrder> listData = new List<SaleOrder> ();
		string pathToDatabase;
		string compCode;
		string branchCode;
		BluetoothAdapter mBluetoothAdapter;
		BluetoothSocket mmSocket;
		BluetoothDevice mmDevice;
		//Thread workerThread;
		//Stream mmOutputStream;
		AdPara apara=null;
		CompanyInfo compinfo;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			// Create your application here
			SetContentView (Resource.Layout.ListView);
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;

			populate (listData);
			apara =  DataHelper.GetAdPara (pathToDatabase,compCode,branchCode);
			listView = FindViewById<ListView> (Resource.Id.feedList);
			Button butNew= FindViewById<Button> (Resource.Id.butnewInv); 
			butNew.Click += butCreateNewInv;
			Button butInvBack= FindViewById<Button> (Resource.Id.butInvBack); 
			butInvBack.Click += (object sender, EventArgs e) => {
				StartActivity(typeof(TransactionsActivity));
			};

			listView.ItemClick += OnListItemClick;
			listView.ItemLongClick += OnListItemLongClick;
			//listView.Adapter = new CusotmListAdapter(this, listData);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<SaleOrder> (this, listData, Resource.Layout.ListItemRow, viewdlg);

		}

		public override void OnBackPressed() {
			// do nothing.
		}

		private void SetViewDelegate(View view,object clsobj)
		{
			SaleOrder item = (SaleOrder)clsobj;
			view.FindViewById<TextView> (Resource.Id.invdate).Text = item.sodate.ToString ("dd-MM-yy");
			view.FindViewById<TextView> (Resource.Id.invno).Text = item.sono;
			view.FindViewById<TextView> (Resource.Id.trxtype).Text = item.custpono;
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
			listData = new List<SaleOrder> ();
			populate (listData);
					apara =  DataHelper.GetAdPara (pathToDatabase,compCode,branchCode);
			listView = FindViewById<ListView> (Resource.Id.feedList);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<SaleOrder> (this, listData, Resource.Layout.ListItemRow, viewdlg);
		}

		void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e) {
			SaleOrder item = listData.ElementAt (e.Position);
			var intent = new Intent(this, typeof(SOItemActivity));
			intent.PutExtra ("invoiceno",item.sono );
			intent.PutExtra ("custcode",item.custcode );
			StartActivity(intent);
		}

		void OnListItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e) {
			SaleOrder item = listData.ElementAt (e.Position);
			PopupMenu menu = new PopupMenu (e.Parent.Context, e.View);
			menu.Inflate (Resource.Menu.popupInv);

			if (!compinfo.AllowDelete) {
				menu.Menu.RemoveItem (Resource.Id.popInvdelete);
			}
			if (DataHelper.GetSaleOrderPrintStatus (pathToDatabase, item.sono,compCode,branchCode)) {
				menu.Menu.RemoveItem (Resource.Id.popInvdelete);
			}
			menu.MenuItemClick += (s1, arg1) => {
				if (arg1.Item.TitleFormatted.ToString().ToLower()=="add")
				{
					CreateNewSaleOrder();
				}else if (arg1.Item.TitleFormatted.ToString().ToLower()=="print")
				{
					PrintInv(item,1);	
				}else if (arg1.Item.TitleFormatted.ToString().ToLower()=="print 2 copy")
				{
					PrintInv(item,2);	

				} else if (arg1.Item.TitleFormatted.ToString().ToLower()=="delete")
				{
					Delete(item);
				}

			};
			menu.Show ();
		}

		void Delete(SaleOrder inv)
		{
			var builder = new AlertDialog.Builder(this);
			builder.SetMessage("Confimr to Delete?");
			builder.SetPositiveButton("YES", (s, e) => { DeleteItem(inv); });
			builder.SetNegativeButton("Cancel", (s, e) => { /* do something on Cancel click */ });
			builder.Create().Show();
		}
		void DeleteItem(SaleOrder inv)
		{
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list = db.Table<SaleOrder>().Where(x=>x.sono==inv.sono&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<SaleOrder>();
				if (list.Count > 0) {
					db.Delete (list [0]);
					var arrlist= listData.Where(x=>x.sono==inv.sono).ToList<SaleOrder>();
					if (arrlist.Count > 0) {
						listData.Remove (arrlist [0]);
						SetViewDlg viewdlg = SetViewDelegate;
						listView.Adapter = new GenericListAdapter<SaleOrder> (this, listData, Resource.Layout.ListItemRow, viewdlg);
					}
				}
			}
		}

		void populate(List<SaleOrder> list)
		{
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list2 = db.Table<SaleOrder> ()
					.Where (x => x.isUploaded == false && x.CompCode == compCode && x.BranchCode == branchCode)
					.OrderByDescending(x=>x.sono)
					.ToList<SaleOrder> ();
				foreach(var item in list2)
				{
					list.Add(item);
				}

			}
			compinfo = DataHelper.GetCompany (pathToDatabase,compCode,branchCode);
		}


		private void butCreateNewInv(object sender, EventArgs e)
		{
			CreateNewSaleOrder ();
		}

		private void CreateNewSaleOrder()
		{
			var intent = new Intent(this, typeof(CreateSaleOrder));
			StartActivity(intent);
		}


		void PrintInv(SaleOrder inv,int noofcopy)
		{
			//Toast.MakeText (this, "print....", ToastLength.Long).Show ();	
			SaleOrderDtls[] list;
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)){
				var ls= db.Table<SaleOrderDtls> ().Where (x => x.sono==inv.sono&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<SaleOrderDtls>();
				list = new SaleOrderDtls[ls.Count];
				ls.CopyTo (list);
			}
			mmDevice = null;
			findBTPrinter ();

			if (mmDevice != null) {
				StartPrint (inv, list,noofcopy);
				updatePrintedStatus (inv);
			}
		
		}

		void updatePrintedStatus(SaleOrder inv)
		{
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var list = db.Table<SaleOrder> ().Where (x => x.sono == inv.sono&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<SaleOrder> ();
				if (list.Count > 0) {
					list [0].isPrinted = true;
					db.Update (list [0]);
				}
			}
		}

		void StartPrint(SaleOrder so,SaleOrderDtls[] list,int noofcopy )
		{
			string userid = ((GlobalvarsApp)this.Application).USERID_CODE;
							PrintInvHelper prnHelp = new PrintInvHelper (pathToDatabase, userid,compCode,branchCode);
			string msg =prnHelp.OpenBTAndPrintSO (mmSocket, mmDevice, so, list,noofcopy);
			Toast.MakeText (this, msg, ToastLength.Long).Show ();	
			//AlertShow (msg);
		}

//		string getBTAddrFile(string printername)
//		{
//			var documents = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
//			string filename = Path.Combine (documents, printername+".baddr");
//			return filename;
//		}

		bool tryConnectBtAddr(string btAddrfile)
		{
			bool found = false;
			if (!File.Exists (btAddrfile))
				return false;
			try{
			mBluetoothAdapter = BluetoothAdapter.DefaultAdapter;
			mmDevice = mBluetoothAdapter.GetRemoteDevice (File.ReadAllBytes (btAddrfile));
			if (mmDevice != null) {
				found = true;
			}
			
			}catch(Exception ex) {
			
			}
			return found;
	     }

		void AlertShow(string text)
		{
			AlertDialog.Builder alert = new AlertDialog.Builder (this);

			alert.SetMessage (text);
			RunOnUiThread (() => {
				alert.Show();
			} );
			
		}
		void findBTPrinter(){
			string printername = apara.PrinterName.Trim ().ToUpper ();
			Utility util = new Utility ();
			string msg = "";
			mmDevice = util.FindBTPrinter (printername,ref  msg);
			Toast.MakeText (this, msg, ToastLength.Long).Show ();	
		}
	}
}

