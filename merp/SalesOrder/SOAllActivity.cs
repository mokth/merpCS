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
	[Activity (Label = "SALES ORDER LIST")]			
	public class SOAllActivity : Activity
	{
		ListView listView ;
		List<SaleOrder> listData = new List<SaleOrder> ();
		string pathToDatabase;
		BluetoothAdapter mBluetoothAdapter;
		BluetoothSocket mmSocket;
		BluetoothDevice mmDevice;
		//Thread workerThread;
		Stream mmOutputStream;
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
			populate (listData);
			apara =  DataHelper.GetAdPara (pathToDatabase);
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
			listView.Adapter = new GenericListAdapter<SaleOrder> (this, listData, Resource.Layout.ListItemRow, viewdlg);

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
		}

		protected override void OnResume()
		{
			base.OnResume();
			listData = new List<SaleOrder> ();
			populate (listData);
			apara =  DataHelper.GetAdPara (pathToDatabase);
			listView = FindViewById<ListView> (Resource.Id.feedList);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<SaleOrder> (this, listData, Resource.Layout.ListItemRow, viewdlg);
		}

		void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e) {
			SaleOrder item = listData.ElementAt (e.Position);
			var intent = new Intent(this, typeof(SOItemHisActivity));
			intent.PutExtra ("invoiceno",item.sono );
			intent.PutExtra ("custcode",item.custcode );
			StartActivity(intent);
		}

		void OnListItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e) {
			SaleOrder item = listData.ElementAt (e.Position);
			PopupMenu menu = new PopupMenu (e.Parent.Context, e.View);
			menu.Inflate (Resource.Menu.popupInv);
			menu.Menu.RemoveItem (Resource.Id.popInvdelete);
			menu.Menu.RemoveItem (Resource.Id.popInvadd);
			menu.MenuItemClick += (s1, arg1) => {
				
				if (arg1.Item.TitleFormatted.ToString ().ToLower () == "print") {
					PrintInv (item,1);	
				}else if (arg1.Item.TitleFormatted.ToString ().ToLower () == "print 2 copy") {
					PrintInv (item,2);	
				}  
			};
			menu.Show ();
		}

		void populate(List<SaleOrder> list)
		{
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list2 = db.Table<SaleOrder>().Where(x=>x.isUploaded==true).ToList<SaleOrder>();
				foreach(var item in list2)
				{
					list.Add(item);
				}

			}
			compinfo = DataHelper.GetCompany (pathToDatabase);
		}


		void PrintInv(SaleOrder so,int noofcopy)
		{
			Toast.MakeText (this, "print....", ToastLength.Long).Show ();	
			SaleOrderDtls[] list;
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)){
				var ls= db.Table<SaleOrderDtls> ().Where (x => x.sono==so.sono).ToList<SaleOrderDtls>();
				list = new SaleOrderDtls[ls.Count];
				ls.CopyTo (list);
			}
			mmDevice = null;
			findBTPrinter ();
			if (mmDevice != null) {
				StartPrint (so, list,noofcopy);
			}
		}

		void StartPrint(SaleOrder so,SaleOrderDtls[] list,int noofcopy )
		{
			string userid = ((GlobalvarsApp)this.Application).USERID_CODE;
			PrintInvHelper prnHelp = new PrintInvHelper (pathToDatabase, userid);
			string msg =prnHelp.OpenBTAndPrintSO (mmSocket, mmDevice,so, list,noofcopy);
			Toast.MakeText (this, msg, ToastLength.Long).Show ();	
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

