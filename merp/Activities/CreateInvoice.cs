
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
	[Activity (Label = "NEW INVOICE")]			
	public class CreateInvoice : Activity,IEventListener
	{
		string pathToDatabase;
		string compCode;
		string branchCode;
		List<Trader> items = null;
		ArrayAdapter<String> dataAdapter;
		ArrayAdapter dataAdapter2;
		DateTime _date ;
		AdPara apara = null;
		Spinner spinner;
		EditText ccType ;
		EditText ccNo;


		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			SetContentView (Resource.Layout.CreateInvoice);
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;

			EventManagerFacade.Instance.GetEventManager().AddListener(this);

			// Create your application here
			_date = DateTime.Today;
			spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);
			Spinner spinnerType = FindViewById<Spinner> (Resource.Id.newinv_type);
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

			ccType =  FindViewById<EditText> (Resource.Id.newinv_cctype);
			ccNo =  FindViewById<EditText> (Resource.Id.newinv_ccno);
			ccNo.AfterTextChanged+= CcNo_AfterTextChanged;


			apara =  DataHelper.GetAdPara (pathToDatabase);
			//SqliteConnection.CreateFile(pathToDatabase);
			LoadTrader ();

			List<string> icodes = new List<string> ();
			foreach (Trader item in items) {
				icodes.Add (item.CustCode+" | "+item.CustName);
			}

			dataAdapter = new ArrayAdapter<String> (this, Resource.Layout.spinner_item, icodes);
			dataAdapter2 =ArrayAdapter.CreateFromResource (
							this, Resource.Array.trxtype, Resource.Layout.spinner_item);

			// Drop down layout style - list view with radio button
			dataAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
			dataAdapter2.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

			// attaching data adapter to spinner
			spinner.Adapter =dataAdapter;
			spinnerType.Adapter =dataAdapter2;
		}

		void LoadTrader ()
		{
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				items = db.Table<Trader>().Where(x=>x.CompCode==compCode && x.BranchCode==branchCode).ToList<Trader> ();
			}
		}

		void CcNo_AfterTextChanged (object sender, Android.Text.AfterTextChangedEventArgs e)
		{
			EditText ccNo = (EditText)sender;
			bool isvalid =CCardHelper.IsCardNumberValid (ccNo.Text);
			if (isvalid) {
				CardType ctype = CCardHelper.GetCardType (ccNo.Text);
				ccType.Text = ctype.ToString ();
			} else
				ccType.Text = "INVALID";
	
		}


		public override void OnBackPressed() {
			// do nothing.
		}

		private void butSaveClick(object sender,EventArgs e)
		{
			int count1 = 0;
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				count1 = db.Table<Item>().Count ();
			}
			if (ccNo.Text != "") {
				if (!CCardHelper.IsCardNumberValid (ccNo.Text)) {
					Toast.MakeText (this, "Invalid Credit Card No... ", ToastLength.Long).Show ();	
				}
			}

			if (count1 > 0)
				CreateNewInvoice ();
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
				Spinner spinnerType = FindViewById<Spinner> (Resource.Id.newinv_type);
				int pos = -1;
				string paycode = item.PayCode.ToUpper().Trim();
				if (!string.IsNullOrEmpty (paycode)) {
					if (paycode.Contains ("CASH")|| paycode.Contains ("COD")) {
						pos = dataAdapter2.GetPosition ("CASH");
					} else {
						pos = dataAdapter2.GetPosition ("INVOICE");
					}
				}

//				if (pos > -1) {
//					spinnerType.SetSelection (pos);
//					spinnerType.Enabled = false;
//				}else spinnerType.Enabled = true;

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


		void ShowItemEntry (Invoice inv, string[] codes)
		{
			var intent = new Intent (this, typeof(EntryActivity));
			intent.PutExtra ("invoiceno", inv.invno);
			intent.PutExtra ("customer", codes [1].Trim ());
			intent.PutExtra ("itemuid", "-1");
			intent.PutExtra ("editmode", "NEW");
			StartActivity (intent);
		}

		private void CreateNewInvoice()
		{
			Invoice inv = new Invoice ();
			string comp =((GlobalvarsApp)Application).COMPANY_CODE;
			string brn =((GlobalvarsApp)Application).BRANCH_CODE;
			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
			DateTime invdate = Utility.ConvertToDate (trxdate.Text);
			DateTime tmr = invdate.AddDays (1);
			AdNumDate adNum= DataHelper.GetNumDate (pathToDatabase, invdate);
			Spinner spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);
			Spinner spinner2 = FindViewById<Spinner> (Resource.Id.newinv_type);
			TextView txtinvno =FindViewById<TextView> (Resource.Id.newinv_no);
			TextView custname = FindViewById<TextView> (Resource.Id.newinv_custname);
			EditText ccNo =  FindViewById<EditText> (Resource.Id.newinv_ccno);
			EditText month =  FindViewById<EditText> (Resource.Id.newinv_month);

			string prefix = apara.Prefix.Trim ().ToUpper ();
			if (spinner.SelectedItem == null) {
				Toast.MakeText (this, "No Customer code selected...", ToastLength.Long).Show ();			
				spinner.RequestFocus ();
				return;			
			}

			string[] codes = spinner.SelectedItem.ToString().Split (new char[]{ '|' });
			if (codes.Length == 0)
				return;
			
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {

				string invno = "";
				int runno = adNum.RunNo + 1;
				int currentRunNo =DataHelper.GetLastInvRunNo (pathToDatabase, invdate);
				if (currentRunNo >= runno)
					runno = currentRunNo + 1;
				
				invno =prefix + invdate.ToString("yyMM") + runno.ToString().PadLeft (4, '0');

				txtinvno.Text= invno;
				inv.invdate = invdate;
				inv.trxtype = spinner2.SelectedItem.ToString ();
				inv.created = DateTime.Now;
				inv.invno = invno;
				inv.description = custname.Text;
				inv.amount = 0;
				inv.custcode = codes [0].Trim ();
				inv.isUploaded = false;
				inv.BranchCode = brn;
				inv.CompCode = comp;
				inv.CCardNo = ccNo.Text;
				inv.InstMonth = Convert.ToInt32(month.Text);
				inv.CCardType = ccType.Text;
				txtinvno.Text = invno;
				db.Insert (inv);
				adNum.RunNo = runno;
				if (adNum.ID >= 0)
					db.Update (adNum);
				else
					db.Insert (adNum);
			}

			ShowItemEntry (inv, codes);
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

