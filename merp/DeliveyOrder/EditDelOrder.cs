
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
	[Activity (Label = "EDIT DELIVERY ORDER")]			
	public class EditDelOrder : Activity,IEventListener
	{
		string pathToDatabase;
		string compCode;
		string branchCode;
		List<Trader> items = null;
		ArrayAdapter<String> dataAdapter;
		DateTime _date ;
		AdPara apara = null;
		Spinner spinner;
		DelOrder doInfo;
		string DONO;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			SetContentView (Resource.Layout.CreateDO);
			EventManagerFacade.Instance.GetEventManager().AddListener(this);

			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;

			DONO = Intent.GetStringExtra ("dono") ?? "AUTO";
			doInfo =DataHelper.GetDelOrder (pathToDatabase, DONO, compCode, branchCode);
			if (doInfo == null) {
				base.OnBackPressed ();
			}
			// Create your application here
			_date = DateTime.Today;
			spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);

			Button butSave = FindViewById<Button> (Resource.Id.newinv_bsave);
			Button butCancel = FindViewById<Button> (Resource.Id.newinv_cancel);
			Button butFind = FindViewById<Button> (Resource.Id.newinv_bfind);
			butSave.Text = "SAVE";

			spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs> (spinner_ItemSelected);
			butSave.Click += butSaveClick;
			butCancel.Click += butCancelClick;
			TextView invno =  FindViewById<TextView> (Resource.Id.newinv_no);
			invno.Enabled = false;

			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
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
				items = db.Table<Trader> ().Where(x=>x.CompCode==compCode&&x.BranchCode==branchCode).ToList<Trader> ();
			}

			List<string> icodes = new List<string> ();
			foreach (Trader item in items) {
				icodes.Add (item.CustCode+" | "+item.CustName);
			}

			dataAdapter = new ArrayAdapter<String> (this, Resource.Layout.spinner_item, icodes);
			dataAdapter.SetDropDownViewResource(Resource.Layout.SimpleSpinnerDropDownItemEx);
			spinner.Adapter =dataAdapter;
			LoadData ();
		}

		private void LoadData()
		{
			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
			DateTime invdate = Utility.ConvertToDate (trxdate.Text);
			Spinner spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);
			TextView txtinvno =FindViewById<TextView> (Resource.Id.newinv_no);
			TextView custname = FindViewById<TextView> (Resource.Id.newinv_custname);
			EditText remark =  FindViewById<EditText> (Resource.Id.newinv_remark);

			trxdate.Text = doInfo.dodate.ToString ("dd-MM-yyyy");
			int pos1= dataAdapter.GetPosition (doInfo.custcode+" | "+doInfo.description);
			spinner.SetSelection (pos1);
			custname.Text = doInfo.description;
			txtinvno.Text = doInfo.dono;
			remark.Text = doInfo.remark;

		}

		public override void OnBackPressed() {
			// do nothing.
		}

		private void butSaveClick(object sender,EventArgs e)
		{
			if (SaveDO()){
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


		void ShowItemEntry (DelOrder so, string[] codes)
		{
			var intent = new Intent (this, typeof(DOEntryActivity));
			intent.PutExtra ("invoiceno", so.dono);
			intent.PutExtra ("customer", codes [1].Trim ());
			intent.PutExtra ("itemuid", "-1");
			intent.PutExtra ("editmode", "NEW");
			StartActivity (intent);
		}

		private bool SaveDO()
		{
			bool lSave = false;
			EditText trxdate =  FindViewById<EditText> (Resource.Id.newinv_date);
			Spinner spinner = FindViewById<Spinner> (Resource.Id.newinv_custcode);
			TextView txtinvno =FindViewById<TextView> (Resource.Id.newinv_no);
			TextView custname = FindViewById<TextView> (Resource.Id.newinv_custname);
			EditText remark =  FindViewById<EditText> (Resource.Id.newinv_remark);
			DateTime dodate = Utility.ConvertToDate (trxdate.Text);

			if (spinner.SelectedItem == null) {
				Toast.MakeText (this, "No Customer code selected...", ToastLength.Long).Show ();			
				spinner.RequestFocus ();
				return lSave;				
			}

			string[] codes = spinner.SelectedItem.ToString().Split (new char[]{ '|' });
			if (codes.Length == 0)
				return lSave;
			
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				
				doInfo.dodate = dodate;
				doInfo.description = custname.Text;
				doInfo.remark = remark.Text;
				doInfo.custcode = codes [0].Trim ();
				doInfo.CompCode = compCode;
				doInfo.BranchCode = branchCode;
				db.Update (doInfo);
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

