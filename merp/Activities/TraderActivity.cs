
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

namespace wincom.mobile.erp
{
	[Activity (Label = "CUSTOMER INFO")]			
	public class TraderActivity : Activity
	{
		string pathToDatabase;
		string compCode;
		string branchCode;
		string CUSTCODE;
		string EDITMODE;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			SetContentView (Resource.Layout.TraderInfo);
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;
			CUSTCODE= Intent.GetStringExtra ("custcode") ?? "AUTO";
			EDITMODE= Intent.GetStringExtra ("editmode") ?? "edit";
			Button but = FindViewById<Button> (Resource.Id.btnTrd_OK);
			but.Click+= (object sender, EventArgs e) => 
			{
				base.OnBackPressed();
			};
			Button butSave = FindViewById<Button> (Resource.Id.btnTrd_Save);
			butSave.Click += ButSave_Click;
			if (EDITMODE == "edit")
				LoadData ();
			else
				NewData ();
			// Create your application here
		}



		private void LoadData()
		{
			Trader trd = new Trader ();
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				var list = db.Table<Trader>().Where(x=>x.CustCode==CUSTCODE&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<Trader>();
				if (list.Count > 0) {
					trd = list [0];
				}
			}

			DisplayData (trd); 
		}

		private void DisplayData(Trader trd)
		{
			EditText code = FindViewById<EditText> (Resource.Id.txtcust_code);
			EditText name = FindViewById<EditText> (Resource.Id.txtcust_name);
			EditText addr1 = FindViewById<EditText> (Resource.Id.txtcust_addr1);
			EditText addr2 = FindViewById<EditText> (Resource.Id.txtcust_addr2);
			EditText addr3 = FindViewById<EditText> (Resource.Id.txtcust_addr3);
			EditText addr4 = FindViewById<EditText> (Resource.Id.txtcust_addr4);
			EditText tel = FindViewById<EditText> (Resource.Id.txtcust_tel);
			EditText fax = FindViewById<EditText> (Resource.Id.txtcust_fax);
			EditText gst= FindViewById<EditText> (Resource.Id.txtcust_gst);
			EditText nric = FindViewById<EditText> (Resource.Id.txtcust_nric);
			code.Text = trd.CustCode;
			code.Enabled = false;
			name.Text = trd.CustName;
			addr1.Text = trd.Addr1;
			addr2.Text = trd.Addr2;
			addr3.Text = trd.Addr3;
			addr4.Text = trd.Addr4;
			tel.Text = trd.Tel;
			fax.Text =trd.Fax;
			gst.Text =trd.gst;
			nric.Text = trd.NRIC;

		}

		private void NewData()
		{
			EditText code = FindViewById<EditText> (Resource.Id.txtcust_code);
			code.Enabled = true;
			code.SetTextColor (Android.Graphics.Color.Black);
		}

		void ButSave_Click (object sender, EventArgs e)
		{
			if (SaveTrader ()) {
				base.OnBackPressed();
			}

		}

		private bool SaveTrader()
		{
			bool isSave = false;
			try {
				string comp = ((GlobalvarsApp)Application).COMPANY_CODE;
				string brn = ((GlobalvarsApp)Application).BRANCH_CODE;
				EditText code = FindViewById<EditText> (Resource.Id.txtcust_code);
				EditText name = FindViewById<EditText> (Resource.Id.txtcust_name);
				EditText addr1 = FindViewById<EditText> (Resource.Id.txtcust_addr1);
				EditText addr2 = FindViewById<EditText> (Resource.Id.txtcust_addr2);
				EditText addr3 = FindViewById<EditText> (Resource.Id.txtcust_addr3);
				EditText addr4 = FindViewById<EditText> (Resource.Id.txtcust_addr4);
				EditText tel = FindViewById<EditText> (Resource.Id.txtcust_tel);
				EditText fax = FindViewById<EditText> (Resource.Id.txtcust_fax);
				EditText gst = FindViewById<EditText> (Resource.Id.txtcust_gst);
				EditText nric = FindViewById<EditText> (Resource.Id.txtcust_nric);

				Trader trd = new Trader ();
				trd.CustCode = code.Text.ToUpper ();
				trd.Addr1 = addr1.Text.ToUpper ();
				trd.Addr2 = addr2.Text.ToUpper ();
				trd.Addr3 = addr3.Text.ToUpper ();
				trd.Addr4 = addr4.Text.ToUpper ();
				trd.CustName = name.Text.ToUpper ();
				trd.Fax = fax.Text.ToUpper ();
				trd.Tel = tel.Text.ToUpper ();
				trd.gst = gst.Text.ToUpper ();
				trd.CompCode= comp;
				trd.BranchCode= brn;
				trd.NRIC= nric.Text.ToUpper();

				using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
					var list = db.Table<Trader> ().Where (x => x.CustCode == code.Text && x.CompCode == comp && x.BranchCode == brn).ToList ();
					if (list.Count > 0) {
						trd.PayCode = list [0].PayCode;
						trd.IsLocal = list [0].IsLocal;
						db.Update (trd);
					} else {
						trd.PayCode = "CASH";
						trd.IsLocal = true;
						db.Insert (trd);
					}
					isSave = true;
				}

			} catch (Exception ex) {
			 
			}
		
			return isSave;
		}
	}


}

