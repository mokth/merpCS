
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
	[Activity (Label = "EDIT INVOICE")]			
	public class EditInvoice : Activity,IEventListener
	{
		string pathToDatabase;
		string compCode;
		string branchCode;
		List<Trader> custs = null;
		ArrayAdapter<String> dataAdapter;
		ArrayAdapter dataAdapter2;
		DateTime _date ;
		AdPara apara = null;
		Spinner spinner;
		string INVOICENO="";
		Invoice invInfo;
		EditText ccType ;
		EditText ccNo;
		string _ccNumber="";

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			SetContentView (Resource.Layout.CreateInvoice);
			EventManagerFacade.Instance.GetEventManager().AddListener(this);
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;

			INVOICENO = Intent.GetStringExtra ("invoiceno") ?? "AUTO";
			invInfo =DataHelper.GetInvoice (pathToDatabase, INVOICENO, compCode, branchCode);
			if (invInfo == null) {
				base.OnBackPressed ();
			}
			// Create your application here

			spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);
			Spinner spinnerType = FindViewById<Spinner> (Resource.Id.newinv_type);
			Button butSave = FindViewById<Button> (Resource.Id.newinv_bsave);
			butSave.Text = "SAVE";
			Button butCancel = FindViewById<Button> (Resource.Id.newinv_cancel);
			Button butFind = FindViewById<Button> (Resource.Id.newinv_bfind);
			spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs> (spinner_ItemSelected);

			butSave.Click += butSaveClick;
			butCancel.Click += butCancelClick;

			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
 			trxdate.Click += delegate(object sender, EventArgs e) {
				ShowDialog (0);
			};
			butFind.Click+= (object sender, EventArgs e) => {
				ShowCustLookUp();
			};

			ccType =  FindViewById<EditText> (Resource.Id.newinv_cctype);
			ccNo =  FindViewById<EditText> (Resource.Id.newinv_ccno);
			ccNo.AfterTextChanged+= CcNo_AfterTextChanged;

			apara =  DataHelper.GetAdPara (pathToDatabase,compCode,branchCode);
			LoadTrader ();

			List<string> icodes = new List<string> ();
			foreach (Trader item in custs) {
				icodes.Add (item.CustCode+" | "+item.CustName);
			}

			dataAdapter = new ArrayAdapter<String> (this, Resource.Layout.spinner_item, icodes);
			dataAdapter2 =ArrayAdapter.CreateFromResource (
							this, Resource.Array.trxtype, Resource.Layout.spinner_item);
			// Drop down layout style - list view with radio button
			dataAdapter.SetDropDownViewResource(Resource.Layout.SimpleSpinnerDropDownItemEx);
			dataAdapter2.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

			// attaching data adapter to spinner
			spinner.Adapter =dataAdapter;
			spinnerType.Adapter =dataAdapter2;
			LoadData ();
		}

		void LoadTrader ()
		{
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				custs = db.Table<Trader> ().Where(x=>x.CompCode==compCode&&x.BranchCode==branchCode).ToList();
			}
		}

		public override void OnBackPressed() {
			// do nothing.
		}

		private void LoadData()
		{
			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
			DateTime invdate = Utility.ConvertToDate (trxdate.Text);
			DateTime tmr = invdate.AddDays (1);
			Spinner spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);
			Spinner spinner2 = FindViewById<Spinner> (Resource.Id.newinv_type);
			TextView txtinvno =FindViewById<TextView> (Resource.Id.newinv_no);
			TextView custname = FindViewById<TextView> (Resource.Id.newinv_custname);
			EditText ccNo =  FindViewById<EditText> (Resource.Id.newinv_ccno);
			EditText month =  FindViewById<EditText> (Resource.Id.newinv_month);
			EditText bank = FindViewById<EditText> (Resource.Id.newinv_bank);

			trxdate.Text = invInfo.invdate.ToString ("dd-MM-yyyy");
			int pos1= dataAdapter.GetPosition (invInfo.custcode+" | "+invInfo.description);
			int pos2= dataAdapter2.GetPosition (invInfo.trxtype);
			spinner.SetSelection (pos1);
			spinner2.SetSelection (pos2);
			custname.Text = invInfo.description;
			txtinvno.Text = invInfo.invno;
			ccNo.Text = invInfo.CCardNo;
			month.Text = invInfo.InstMonth.ToString ();
			_ccNumber = invInfo.CCardNo;
			bank.Text = invInfo.CCBank;
		}

		void CcNo_AfterTextChanged (object sender, Android.Text.AfterTextChangedEventArgs e)
		{
			EditText ccNo = (EditText)sender;


			if(_ccNumber.Length < ccNo.Text.Length){

				switch(ccNo.Text.Length){
				case 5:
					ccNo.Text =ccNo.Text.Insert(4, " ");
					break;
				case 10:
					ccNo.Text= ccNo.Text.Insert(9, " ");
					break;
				case 15:
					ccNo.Text=ccNo.Text.Insert(14, " ");
					break;
				}
			}
			ccNo.SetSelection(ccNo.Text.Length);
			_ccNumber = ccNo.Text;

			bool isvalid =CCardHelper.IsCardNumberValid (ccNo.Text);
			if (isvalid) {
				CardType ctype = CCardHelper.GetCardType (ccNo.Text);
				ccType.Text = ctype.ToString ();
			} else
				ccType.Text = "INVALID";

		}

		private void butSaveClick(object sender,EventArgs e)
		{
			if (ccNo.Text != "") {
				if (!CCardHelper.IsCardNumberValid (ccNo.Text)) {
					Toast.MakeText (this, "Invalid Credit Card No... ", ToastLength.Long).Show ();	
				}
			}


			if (SaveInvoice()){
				base.OnBackPressed();	
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
			
			Trader item =custs.Where (x => x.CustCode ==codes[0].Trim()).FirstOrDefault ();
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
					

		private bool SaveInvoice()
		{
			bool lSave = false;
			Invoice inv = new Invoice ();
			string comp =((GlobalvarsApp)Application).COMPANY_CODE;
			string brn =((GlobalvarsApp)Application).BRANCH_CODE;
			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
			DateTime invdate = Utility.ConvertToDate (trxdate.Text);

			Spinner spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);
			Spinner spinner2 = FindViewById<Spinner> (Resource.Id.newinv_type);
			TextView txtinvno =FindViewById<TextView> (Resource.Id.newinv_no);
			TextView custname = FindViewById<TextView> (Resource.Id.newinv_custname);
			EditText ccNo =  FindViewById<EditText> (Resource.Id.newinv_ccno);
			EditText month =  FindViewById<EditText> (Resource.Id.newinv_month);
			EditText bank = FindViewById<EditText> (Resource.Id.newinv_bank);

			if (month.Text.Trim () == "") {

				Toast.MakeText (this,"The installment plan month is empty...", ToastLength.Long).Show ();	
				return lSave;	
			}

			if (spinner.SelectedItem == null) {
				Toast.MakeText (this, "No Customer code selected...", ToastLength.Long).Show ();			
				spinner.RequestFocus ();
				return lSave;			
			}

			string[] codes = spinner.SelectedItem.ToString().Split (new char[]{ '|' });
			if (codes.Length == 0)
				return lSave;
			
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {

				invInfo.invdate = invdate;
				invInfo.trxtype = spinner2.SelectedItem.ToString ();
				invInfo.created = DateTime.Now;
				invInfo.description = custname.Text;
				//inv.amount = 0;
				invInfo.custcode = codes [0].Trim ();
				invInfo.isUploaded = false;
				invInfo.BranchCode = brn;
				invInfo.CompCode = comp;
				invInfo.CCardNo = ccNo.Text;
				invInfo.InstMonth = Convert.ToInt32(month.Text);
				invInfo.CCBank = bank.Text;
				db.Update (invInfo);
				lSave = true;
			}

			return lSave;
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

