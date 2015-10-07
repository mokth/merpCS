
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
	[Activity (Label = "DELIVERY ORDER ITEM(S)")]			
	public class DOItemActivity : Activity
	{
		ListView listView ;
		List<DelOrderDtls> listData = new List<DelOrderDtls> ();
		string pathToDatabase;
		string compCode;
		string branchCode;
		string dono ="";
		string CUSTCODE ="";
		string CUSTNAME ="";
		string EDITMODE="";
		CompanyInfo comp;
		bool isNotAllowEditAfterPrinted  ;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;
			SetContentView (Resource.Layout.InvDtlView);
			dono = Intent.GetStringExtra ("invoiceno") ?? "AUTO";
			CUSTCODE = Intent.GetStringExtra ("custcode") ?? "AUTO";
			EDITMODE = Intent.GetStringExtra ("editmode") ?? "AUTO";

			isNotAllowEditAfterPrinted  = DataHelper.GetDelOderPrintStatus (pathToDatabase,dono,compCode,branchCode);
			Button butNew= FindViewById<Button> (Resource.Id.butnewItem); 
			butNew.Click += (object sender, EventArgs e) => {
				NewItem(dono);
			};
			if (isNotAllowEditAfterPrinted)
				butNew.Enabled = false;
		   
			Button butInvBack= FindViewById<Button> (Resource.Id.butInvItmBack); 
			butInvBack.Click += (object sender, EventArgs e) => {
				if (EDITMODE.ToLower()=="new")
				{
					DeleteDOWithEmptyDOitem();
				}
				StartActivity(typeof(DelOrderActivity));
			};


			populate (listData);
			listView = FindViewById<ListView> (Resource.Id.invitemList);
			listView.ItemClick += OnListItemClick;
			//listView.Adapter = new CusotmItemListAdapter(this, listData);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<DelOrderDtls> (this, listData, Resource.Layout.InvDtlItemView, viewdlg);
		}
		public override void OnBackPressed() {
			// do nothing.
		}

		private void DeleteDOWithEmptyDOitem()
		{
			try{
				using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
					var list = db.Table<DelOrderDtls>().Where(x=>x.dono==dono&&x.CompCode==compCode&&branchCode==branchCode).ToList<DelOrderDtls>();
					if (list.Count == 0) {
						var list2 = db.Table<DelOrder>().Where(x=>x.dono==dono&&x.CompCode==compCode&&branchCode==branchCode).ToList<DelOrder>();
						if (list2.Count > 0) {
							AdNumDate adNum= DataHelper.GetNumDate(pathToDatabase, list2[0].dodate,"DO",compCode,branchCode);
							if (dono.Length > 5) {
								string snum= dono.Substring (dono.Length - 4);					
								int num;
								if (int.TryParse (snum, out num)) {
									if (adNum.RunNo == num) {
										adNum.RunNo = num - 1;
										db.Delete (list2[0]);
										db.Delete (adNum);
									}
								}
							}
						}
						//db.Table<Invoice> ().Delete (x => x.invno == invno);
					}
				}
			}catch{
			}
		}

		private void SetViewDelegate(View view,object clsobj)
		{
			DelOrderDtls item = (DelOrderDtls)clsobj;
			string sqty =item.qty==0?"": item.qty.ToString ();
			string sprice =item.price==0?"": item.price.ToString ("n2");

			view.FindViewById<TextView> (Resource.Id.invitemcode).Text = item.icode;
			view.FindViewById<TextView> (Resource.Id.invitemdesc).Text = item.description;
			view.FindViewById<TextView> (Resource.Id.invitemqty).Text = "";
			view.FindViewById<TextView> (Resource.Id.invitemtaxgrp).Text = "";
			view.FindViewById<TextView> (Resource.Id.invitemtax).Text = "";
			view.FindViewById<TextView> (Resource.Id.invitemprice).Text = "";
			view.FindViewById<TextView> (Resource.Id.invitemamt).Text = sqty;


		}

		protected override void OnResume()
		{
			base.OnResume();
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			dono = Intent.GetStringExtra ("invoiceno") ?? "AUTO";
			CUSTCODE = Intent.GetStringExtra ("custcode") ?? "AUTO";
			isNotAllowEditAfterPrinted  = DataHelper.GetDelOderPrintStatus (pathToDatabase,dono,compCode,branchCode);
			Button butNew= FindViewById<Button> (Resource.Id.butnewItem); 
			if (isNotAllowEditAfterPrinted)
				butNew.Enabled = false;
			
			listData = new List<DelOrderDtls> ();
			populate (listData);
			listView = FindViewById<ListView> (Resource.Id.invitemList);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<DelOrderDtls> (this, listData, Resource.Layout.InvDtlItemView, viewdlg);
		}

		void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e) {
			DelOrderDtls item = listData.ElementAt (e.Position);
			if (item.icode.IndexOf ("TOTAL") > -1)
				return;
			
			PopupMenu menu = new PopupMenu (e.Parent.Context, e.View);
			menu.Inflate (Resource.Menu.popupItem);
			if (!comp.AllowEdit) {
				menu.Menu.RemoveItem (Resource.Id.popedit);
			}
			if (!comp.AllowDelete) {
				menu.Menu.RemoveItem (Resource.Id.popdelete);
			}
			//if allow edit and DelOrder printed, remove edit
			//printed invoice not allow to edit

			if (isNotAllowEditAfterPrinted) {
				menu.Menu.RemoveItem (Resource.Id.popedit);
				menu.Menu.RemoveItem (Resource.Id.popdelete);
				menu.Menu.RemoveItem (Resource.Id.popadd);
			}


			menu.MenuItemClick += (s1, arg1) => {
				if (arg1.Item.TitleFormatted.ToString().ToLower()=="add")
				{
					NewItem(item.dono);
				}else if (arg1.Item.TitleFormatted.ToString().ToLower()=="edit")
				{
					Edit(item);
				} else if (arg1.Item.TitleFormatted.ToString().ToLower()=="delete")
				{
					Delete(item);
				}
			
			};
			menu.Show ();

		}

		void Delete(DelOrderDtls so)
		{
			var builder = new AlertDialog.Builder(this);
			builder.SetMessage("Confimr to Delete?");
			builder.SetPositiveButton("YES", (s, e) => { DeleteItem(so); });
			builder.SetNegativeButton("Cancel", (s, e) => { /* do something on Cancel click */ });
			builder.Create().Show();
		}
		void DeleteItem(DelOrderDtls so)
		{
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list = db.Table<DelOrderDtls>().Where(x=>x.dono==dono&& x.ID==so.ID).ToList<DelOrderDtls>();
				if (list.Count > 0) {
					db.Delete (list [0]);
					var arrlist= listData.Where(x=>x.dono==dono&& x.ID==so.ID).ToList<DelOrderDtls>();
					if (arrlist.Count > 0) {
						listData.Remove (arrlist [0]);
						SetViewDlg viewdlg = SetViewDelegate;
						listView.Adapter = new GenericListAdapter<DelOrderDtls> (this, listData, Resource.Layout.InvDtlItemView, viewdlg);
					}
				}
			}
		}
		void Edit(DelOrderDtls so)
		{
			var intent = new Intent(this, typeof(DOEntryActivity));
			intent.PutExtra ("invoiceno",so.dono );
			intent.PutExtra ("itemuid",so.ID.ToString() );
			intent.PutExtra ("editmode","EDIT" );
			intent.PutExtra ("customer",CUSTNAME );
			intent.PutExtra ("custcode",CUSTCODE );
			StartActivity(intent);
		}

		private void NewItem(string dono)
		{
			var intent = new Intent(this, typeof(DOEntryActivity));
			intent.PutExtra ("invoiceno",dono );
			intent.PutExtra ("itemuid","-1");
			intent.PutExtra ("editmode","NEW" );
			intent.PutExtra ("customer",CUSTNAME );
			intent.PutExtra ("custcode",CUSTCODE );
			StartActivity(intent);
		}

		void populate(List<DelOrderDtls> list)
		{
			
			comp = DataHelper.GetCompany (pathToDatabase,compCode,branchCode);

			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list1 = db.Table<DelOrder>().Where(x=>x.dono==dono&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<DelOrder>();
				var list2 = db.Table<DelOrderDtls>().Where(x=>x.dono==dono&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<DelOrderDtls>();
				var list3 = db.Table<Trader>().Where(x=>x.CustCode==CUSTCODE&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<Trader>();

				double ttlamt = 0;
				double ttltax = 0;
				if (list3.Count > 0) {
					CUSTNAME = list3 [0].CustName;
				}
				foreach(var item in list2)
				{
					ttlamt = ttlamt + item.qty;
					ttltax = ttltax + item.tax;
					list.Add(item);
				}
				if (list1.Count > 0) {
					list1 [0].amount = ttlamt;
					db.Update (list1 [0]);
				}

				DelOrderDtls inv2 = new DelOrderDtls ();
				inv2.icode = "TOTAL";
				inv2.qty = ttlamt;

				list.Add (inv2);
			}
		}
	}
}

