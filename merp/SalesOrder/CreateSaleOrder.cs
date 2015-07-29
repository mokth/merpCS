
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
	[Activity (Label = "NEW SALES ORDER")]			
	public class CreateSaleOrder : Activity,IEventListener
	{
		string pathToDatabase;
		string compCode;
		string branchCode;
		List<Trader> items = null;
		ArrayAdapter<String> dataAdapter;
		DateTime _date ;
		AdPara apara = null;
		Spinner spinner;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			SetContentView (Resource.Layout.CreateSO);
			EventManagerFacade.Instance.GetEventManager().AddListener(this);

			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;
			// Create your application here
			_date = DateTime.Today;
			spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);

			Button butSave = FindViewById<Button> (Resource.Id.newinv_bsave);
			Button butNew = FindViewById<Button> (Resource.Id.newinv_cancel);
			Button butFind = FindViewById<Button> (Resource.Id.newinv_bfind);
			spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs> (spinner_ItemSelected);
			butSave.Click += butSaveClick;
			butNew.Click += butCancelClick;
			TextView invno =  FindViewById<TextView> (Resource.Id.newinv_no);
			invno.Text = "AUTO";
			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
 			trxdate.Text = _date.ToString ("dd-MM-yyyy");
			trxdate.Click += delegate(object sender, EventArgs e) {
				ShowDialog (0);
			};
			butFind.Click+= (object sender, EventArgs e) => {
				ShowCustLookUp();
			};


			apara =  DataHelper.GetAdPara (pathToDatabase,compCode,branchCode);
			//SqliteConnection.CreateFile(pathToDatabase);
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				items = db.Table<Trader>().Where(x=>x.CompCode==compCode && x.BranchCode==branchCode).ToList<Trader> ();
			}

			List<string> icodes = new List<string> ();
			foreach (Trader item in items) {
				icodes.Add (item.CustCode+" | "+item.CustName);
			}

			dataAdapter = new ArrayAdapter<String> (this, Resource.Layout.spinner_item, icodes);
			dataAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
			spinner.Adapter =dataAdapter;

		}

		public override void OnBackPressed() {
			// do nothing.
		}

		private void butSaveClick(object sender,EventArgs e)
		{
			int count1 = 0;
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				count1 = db.Table<Item>().Where(x=>x.CompCode==compCode&&x.BranchCode==branchCode).Count ();
			}
			if (count1 > 0)
				CreateNewSO ();
			else {
				Toast.MakeText (this,"Please download Master Item before proceed... ", ToastLength.Long).Show ();	
			}
		}

		private void butCancelClick(object sender,EventArgs e)
		{
			base.OnBackPressed();
		}

		private void spinner_ItemSelected (object sender, AdapterView.ItemSelectedEventArgs e)
		{
			Spinner spinner = (Spinner)sender;

			string txt = spinner.GetItemAtPosition (e.Position).ToString();
			string[] codes = txt.Split (new char[]{ '|' });
			if (codes.Length == 0)
				return;
			
			Trader item =items.Where (x => x.CustCode ==codes[0].Trim()).FirstOrDefault ();
			if (item != null) {
				TextView name = FindViewById<TextView> (Resource.Id.newinv_custname);
				name.Text = item.CustName;

			}

		}
		[Obsolete]
		protected override Dialog OnCreateDialog (int id)
		{
			return new DatePickerDialog (this, HandleDateSet, _date.Year,
				_date.Month - 1, _date.Day);
		}

		void HandleDateSet (object sender, DatePickerDialog.DateSetEventArgs e)
		{
			_date = e.Date;
			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
			trxdate.Text = _date.ToString ("dd-MM-yyyy");
		}


		void ShowItemEntry (SaleOrder so, string[] codes)
		{
			var intent = new Intent (this, typeof(SOEntryActivity));
			intent.PutExtra ("invoiceno", so.sono);
			intent.PutExtra ("customer", codes [1].Trim ());
			intent.PutExtra ("itemuid", "-1");
			intent.PutExtra ("editmode", "NEW");
			StartActivity (intent);
		}

		private void CreateNewSO()
		{
			SaleOrder so = new SaleOrder ();
			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
			DateTime sodate = Utility.ConvertToDate (trxdate.Text);
			DateTime tmr = sodate.AddDays (1);
			AdNumDate adNum= DataHelper.GetNumDate(pathToDatabase, sodate,"SO",compCode,branchCode);
			Spinner spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);
			TextView txtinvno =FindViewById<TextView> (Resource.Id.newinv_no);
			TextView custname = FindViewById<TextView> (Resource.Id.newinv_custname);
			EditText custpo =  FindViewById<EditText> (Resource.Id.newinv_po);
			EditText remark =  FindViewById<EditText> (Resource.Id.newinv_remark);
			string prefix = apara.SOPrefix.Trim ().ToUpper ();
			if (spinner.SelectedItem == null) {
				Toast.MakeText (this, "No Customer code selected...", ToastLength.Long).Show ();			
				spinner.RequestFocus ();
				return;			
			}

			string[] codes = spinner.SelectedItem.ToString().Split (new char[]{ '|' });
			if (codes.Length == 0)
				return;
			
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				string sono = "";
				int runno = adNum.RunNo + 1;
				int currentRunNo =DataHelper.GetLastSORunNo(pathToDatabase, sodate,compCode,branchCode);
				if (currentRunNo >= runno)
					runno = currentRunNo + 1;
				
				sono =prefix + sodate.ToString("yyMM") + runno.ToString().PadLeft (4, '0');

				txtinvno.Text= sono;
				so.sodate = sodate;
				so.trxtype ="SO" ;
				so.created = DateTime.Now;
				so.sono = sono;
				so.description = custname.Text;
				so.remark = remark.Text;
				so.custpono = custpo.Text;
				so.amount = 0;
				so.custcode = codes [0].Trim ();
				so.isUploaded = false;
				so.CompCode = compCode;
				so.BranchCode = branchCode;

				txtinvno.Text = sono;
				db.Insert (so);
				adNum.RunNo = runno;
				if (adNum.ID >= 0)
					db.Update (adNum);
				else
					db.Insert (adNum);
			}

			ShowItemEntry (so, codes);
		}

		void ShowCustLookUp()
		{
			var dialog = TraderDialog.NewInstance();
			dialog.Show(FragmentManager, "dialog");
		}

		void SetSelectedItem(string text)
		{
			int position=dataAdapter.GetPosition (text);
			spinner.SetSelection (position);
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
			case EventID.CUSTCODE_SELECTED:
				RunOnUiThread (() => SetSelectedItem(e.Param["SELECTED"].ToString()));
				break;
			}
		}
	}
}

