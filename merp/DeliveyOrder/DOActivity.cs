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
	[Activity (Label = "DELIVERY ORDER")]			
	public class DelOrderActivity : Activity
	{
		ListView listView ;
		List<DelOrder> listData = new List<DelOrder> ();
		string pathToDatabase;
		BluetoothAdapter mBluetoothAdapter;
		BluetoothDevice mmDevice;
		BluetoothSocket mmSocket;

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
			listView.Adapter = new GenericListAdapter<DelOrder> (this, listData, Resource.Layout.ListItemRow, viewdlg);

		}

		public override void OnBackPressed() {
			// do nothing.
		}

		private void SetViewDelegate(View view,object clsobj)
		{
			DelOrder item = (DelOrder)clsobj;
			view.FindViewById<TextView> (Resource.Id.invdate).Text = item.dodate.ToString ("dd-MM-yy");
			view.FindViewById<TextView> (Resource.Id.invno).Text = item.dono;
			view.FindViewById<TextView> (Resource.Id.trxtype).Text = "";
			view.FindViewById<TextView>(Resource.Id.invcust).Text = item.description;
			//view.FindViewById<TextView> (Resource.Id.Amount).Text = item.amount.ToString("n2");
			view.FindViewById<TextView> (Resource.Id.TaxAmount).Text = "";
			view.FindViewById<TextView> (Resource.Id.TtlAmount).Text = item.amount.ToString("n");;
			ImageView img = view.FindViewById<ImageView> (Resource.Id.printed);
			if (!item.isPrinted)
				img.Visibility = ViewStates.Invisible;
		}

		protected override void OnResume()
		{
			base.OnResume();
			listData = new List<DelOrder> ();
			populate (listData);
			apara =  DataHelper.GetAdPara (pathToDatabase);
			listView = FindViewById<ListView> (Resource.Id.feedList);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<DelOrder> (this, listData, Resource.Layout.ListItemRow, viewdlg);
		}

		void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e) {
			DelOrder item = listData.ElementAt (e.Position);
			var intent = new Intent(this, typeof(DOItemActivity));
			intent.PutExtra ("invoiceno",item.dono );
			intent.PutExtra ("custcode",item.custcode );
			StartActivity(intent);
		}

		void OnListItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e) {
			DelOrder item = listData.ElementAt (e.Position);
			PopupMenu menu = new PopupMenu (e.Parent.Context, e.View);
			menu.Inflate (Resource.Menu.popupInv);

			if (!compinfo.AllowDelete) {
				menu.Menu.RemoveItem (Resource.Id.popInvdelete);
			}
			if (DataHelper.GetDelOderPrintStatus (pathToDatabase, item.dono)) {
				menu.Menu.RemoveItem (Resource.Id.popInvdelete);
			}
			menu.MenuItemClick += (s1, arg1) => {
				if (arg1.Item.TitleFormatted.ToString().ToLower()=="add")
				{
					CreateNewDelOrder();
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

		void Delete(DelOrder inv)
		{
			var builder = new AlertDialog.Builder(this);
			builder.SetMessage("Confimr to Delete?");
			builder.SetPositiveButton("YES", (s, e) => { DeleteItem(inv); });
			builder.SetNegativeButton("Cancel", (s, e) => { /* do something on Cancel click */ });
			builder.Create().Show();
		}
		void DeleteItem(DelOrder doorder)
		{
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list = db.Table<DelOrder>().Where(x=>x.dono==doorder.dono).ToList<DelOrder>();
				if (list.Count > 0) {
					db.Delete (list [0]);
					var arrlist= listData.Where(x=>x.dono==doorder.dono).ToList<DelOrder>();
					if (arrlist.Count > 0) {
						listData.Remove (arrlist [0]);
						SetViewDlg viewdlg = SetViewDelegate;
						listView.Adapter = new GenericListAdapter<DelOrder> (this, listData, Resource.Layout.ListItemRow, viewdlg);
					}
				}
			}
		}

		void populate(List<DelOrder> list)
		{
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list2 = db.Table<DelOrder>().ToList<DelOrder>().Where(x=>x.isUploaded==false);
				foreach(var item in list2)
				{
					list.Add(item);
				}

			}
			compinfo = DataHelper.GetCompany (pathToDatabase);
		}


		private void butCreateNewInv(object sender, EventArgs e)
		{
			CreateNewDelOrder ();
		}

		private void CreateNewDelOrder()
		{
			var intent = new Intent(this, typeof(CreateDelOrder));
			StartActivity(intent);
		}


		void PrintInv(DelOrder inv,int noofcopy)
		{
			//Toast.MakeText (this, "print....", ToastLength.Long).Show ();	
			DelOrderDtls[] list;
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)){
				var ls= db.Table<DelOrderDtls> ().Where (x => x.dono==inv.dono).ToList<DelOrderDtls>();
				list = new DelOrderDtls[ls.Count];
				ls.CopyTo (list);
			}
			mmDevice = null;
			findBTPrinter ();

			if (mmDevice != null) {
				StartPrint (inv, list,noofcopy);
				updatePrintedStatus (inv);
			}
		
		}

		void updatePrintedStatus(DelOrder inv)
		{
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var list = db.Table<DelOrder> ().Where (x => x.dono == inv.dono).ToList<DelOrder> ();
				if (list.Count > 0) {
					list [0].isPrinted = true;
					db.Update (list [0]);
				}
			}
		}

		void StartPrint(DelOrder so,DelOrderDtls[] list,int noofcopy )
		{
			string userid = ((GlobalvarsApp)this.Application).USERID_CODE;
			PrintInvHelper prnHelp = new PrintInvHelper (pathToDatabase, userid);
			string msg =prnHelp.OpenBTAndPrintDO (mmSocket, mmDevice, so, list,noofcopy);
		   Toast.MakeText (this, msg, ToastLength.Long).Show ();	
			//AlertShow (msg);
		}

		string getBTAddrFile(string printername)
		{
			var documents = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
			string filename = Path.Combine (documents, printername+".baddr");
			return filename;
		}

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

