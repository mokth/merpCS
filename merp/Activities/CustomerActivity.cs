

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;

namespace wincom.mobile.erp
{
	
	[Activity (Label = "CUSTOMER PROFILE")]			
	public class CustomerActivity : Activity
	{
		List<Trader> listData = new List<Trader> ();
		ListView listView ;
		string pathToDatabase;
		string compCode;
		string branchCode;
		CompanyInfo compinfo;
		GenericListAdapter<Trader> adapter; 
		EditText  txtSearch;
		SetViewDlg viewdlg;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			SetContentView (Resource.Layout.ListCustViewEx);
			// Create your application here
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode =((GlobalvarsApp)Application).COMPANY_CODE;
			branchCode =((GlobalvarsApp)Application).BRANCH_CODE;
			compinfo = DataHelper.GetCompany (pathToDatabase,compCode,branchCode);

			populate (listData);
			listView = FindViewById<ListView> (Resource.Id.CustList);

			Button butInvBack= FindViewById<Button> (Resource.Id.butCustBack); 
			Button butInvAdd= FindViewById<Button> (Resource.Id.butCustAdd); 
			txtSearch= FindViewById<EditText > (Resource.Id.txtSearch);
			butInvBack.Click += (object sender, EventArgs e) => {
				base.OnBackPressed();
			};

			butInvAdd.Click+= (object sender, EventArgs e) => {
				NewLocalCust();
			};

			listView.ItemClick+= ListView_ItemClick; ;
			listView.ItemLongClick+= ListView_ItemLongClick;
		    //istView.Adapter = new CusotmCustomerListAdapter(this, listData);
			viewdlg = SetViewDelegate;
			adapter = new GenericListAdapter<Trader> (this, listData, Resource.Layout.ListCustDtlView, viewdlg);
			listView.Adapter = adapter;
			txtSearch.TextChanged+= TxtSearch_TextChanged;
		}

		void FindItemByText ()
		{
			List<Trader> found = PerformSearch (txtSearch.Text);
			adapter = new GenericListAdapter<Trader> (this, found, Resource.Layout.ListCustDtlView, viewdlg);
			listView.Adapter = adapter;
		}

		void TxtSearch_TextChanged (object sender, Android.Text.TextChangedEventArgs e)
		{
			FindItemByText ();
		}

		void EditCust (Trader item)
		{
			var intent = new Intent (this, typeof(TraderActivity));
			intent.PutExtra ("custcode", item.CustCode);
			intent.PutExtra ("editmode", "edit");
			StartActivity (intent);
		}

		void NewLocalCust()
		{
			var intent = new Intent (this, typeof(TraderActivity));
			intent.PutExtra ("custcode", "");
			intent.PutExtra ("editmode", "new");

			StartActivity (intent);
		}

		void DeleteCust (Trader item)
		{
			bool isDeleted = false;
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var list = db.Table<Trader> ().Where (x => x.CustCode == item.CustCode && x.CompCode == item.CompCode && x.BranchCode == item.BranchCode).ToList ();
				if (list.Count > 0) {
					if (list [0].IsLocal) {
						db.Delete (list [0]);
						isDeleted = true;
					}
				}
			}

			if (isDeleted) {
				listData.Clear ();
				populate (listData);
				SetViewDlg viewdlg = SetViewDelegate;
				listView.Adapter = new GenericListAdapter<Trader> (this, listData, Resource.Layout.ListCustDtlView, viewdlg);
			}
		}

		void ListView_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
		{
			Trader item = listData.ElementAt (e.Position);
			EditCust (item);
		}

		void ListView_ItemLongClick (object sender, AdapterView.ItemLongClickEventArgs e)
		{
			Trader item = listData.ElementAt (e.Position);
			PopupMenu menu = new PopupMenu (e.Parent.Context, e.View);
			menu.Inflate (Resource.Menu.popupItem);

			if (!compinfo.AllowDelete) {
				menu.Menu.RemoveItem (Resource.Id.popdelete);
			}

			menu.MenuItemClick += (s1, arg1) => {
				if (arg1.Item.TitleFormatted.ToString().ToLower()=="add")
				{
					NewLocalCust();
					
				} else if (arg1.Item.TitleFormatted.ToString().ToLower()=="delete")
				{
					DeleteCust(item);
				}
				else if (arg1.Item.TitleFormatted.ToString().ToLower()=="edit")
				{
					EditCust (item);
				}


			};
			menu.Show ();

		}

		private void SetViewDelegate(View view,object clsobj)
		{
			Trader item = (Trader)clsobj;
			view.FindViewById<TextView> (Resource.Id.custcode).Text = item.CustCode;
			view.FindViewById<TextView> (Resource.Id.custname).Text = item.CustName;

		}
		protected override void OnResume()
		{
			base.OnResume();
			if (txtSearch.Text != "") {
				FindItemByText ();
				return;
			}
			listData.Clear ();

			populate (listData);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<Trader> (this, listData, Resource.Layout.ListCustDtlView, viewdlg);
		}

		void populate(List<Trader> list)
		{

			//SqliteConnection.CreateFile(pathToDatabase);
			//list = new List<Trader>();
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list2 = db.Table<Trader>().Where(x=>x.CompCode==compCode&&x.BranchCode==branchCode).ToList<Trader>();
				foreach(var item in list2)
				{
					list.Add(item);
				}

			}
		}


		List<Trader> PerformSearch (string constraint)
		{
			List<Trader>  results = new List<Trader>();
			if (constraint != null) {
				var searchFor = constraint.ToString ().ToUpper();
				Console.WriteLine ("searchFor:" + searchFor);

				foreach(Trader itm in listData)
				{
					if (itm.CustCode.ToUpper().IndexOf (searchFor) >= 0) {
						results.Add (itm);
						continue;
					}

					if (itm.CustName.ToUpper().IndexOf (searchFor) >= 0) {
						results.Add (itm);
						continue;
					}
				}


			}
			return results;
		}

	}
}

