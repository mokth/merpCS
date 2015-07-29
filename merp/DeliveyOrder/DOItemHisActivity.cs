
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
	[Activity (Label = "DELIVeRY ORDER ITEM(S)")]			
	public class DOItemHisActivity : Activity
	{
		ListView listView ;
		List<DelOrderDtls> listData = new List<DelOrderDtls> ();
		string pathToDatabase;
		string compCode;
		string branchCode;
		string invno ="";
		string CUSTCODE ="";
		string CUSTNAME ="";
	//	CompanyInfo comp;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			SetContentView (Resource.Layout.InvDtlView);
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;

			invno = Intent.GetStringExtra ("invoiceno") ?? "AUTO";
			CUSTCODE = Intent.GetStringExtra ("custcode") ?? "AUTO";

//			TableLayout tlay = FindViewById<TableLayout> (Resource.Id.tableLayout1);
//			tlay.Visibility = ViewStates.Invisible;
			Button butNew= FindViewById<Button> (Resource.Id.butnewItem); 
			butNew.Visibility = ViewStates.Invisible;
			Button butInvBack= FindViewById<Button> (Resource.Id.butInvItmBack); 
			butInvBack.Click+= (object sender, EventArgs e) => {
				base.OnBackPressed();
			};
				
			populate (listData);
			listView = FindViewById<ListView> (Resource.Id.invitemList);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<DelOrderDtls> (this, listData, Resource.Layout.InvDtlItemView, viewdlg);
		}
	
		private void SetViewDelegate(View view,object clsobj)
		{
			DelOrderDtls item = (DelOrderDtls)clsobj;
			string sqty =item.qty==0?"": item.qty.ToString ();
			string sprice =item.price==0?"": item.price.ToString ("n2");

			view.FindViewById<TextView> (Resource.Id.invitemcode).Text = item.icode;
			view.FindViewById<TextView> (Resource.Id.invitemdesc).Text = item.description;
			view.FindViewById<TextView> (Resource.Id.invitemqty).Text = sqty;
			view.FindViewById<TextView> (Resource.Id.invitemtaxgrp).Text = "";
			view.FindViewById<TextView> (Resource.Id.invitemtax).Text = "";
			view.FindViewById<TextView> (Resource.Id.invitemprice).Text = "";
			view.FindViewById<TextView> (Resource.Id.invitemamt).Text = "";


		}

		protected override void OnResume()
		{
			base.OnResume();
			invno = Intent.GetStringExtra ("invoiceno") ?? "AUTO";
			CUSTCODE = Intent.GetStringExtra ("custcode") ?? "AUTO";
			listData = new List<DelOrderDtls> ();
			populate (listData);
			listView = FindViewById<ListView> (Resource.Id.invitemList);
			SetViewDlg viewdlg = SetViewDelegate;
			listView.Adapter = new GenericListAdapter<DelOrderDtls> (this, listData, Resource.Layout.InvDtlItemView, viewdlg);
		}
			
		void populate(List<DelOrderDtls> list)
		{
			//	comp = DataHelper.GetCompany (pathToDatabase,compCode,branchCode);
			//SqliteConnection.CreateFile(pathToDatabase);
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list1 = db.Table<DelOrder>().Where(x=>x.dono==invno&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<DelOrder>();
				var list2 = db.Table<DelOrderDtls>().Where(x=>x.dono==invno&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<DelOrderDtls>();
				var list3 = db.Table<Trader>().Where(x=>x.CustCode==CUSTCODE&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<Trader>();

				double ttlamt = 0;
				double ttltax = 0;
				if (list3.Count > 0) {
					CUSTNAME = list3 [0].CustName;
				}
				foreach(var item in list2)
				{
					ttlamt = ttlamt + item.netamount;
					ttltax = ttltax + item.tax;
					list.Add(item);
				}
				if (list1.Count > 0) {
					list1 [0].amount = ttlamt;
					db.Update (list1 [0]);
				}
				DelOrderDtls inv1 = new DelOrderDtls ();
				inv1.icode = "TAX";
				inv1.netamount = ttltax;
				DelOrderDtls inv2 = new DelOrderDtls ();
				inv2.icode = "AMOUNT";
				inv2.netamount = ttlamt;

				list.Add (inv1);
				list.Add (inv2);
			}
		}
	}
}

