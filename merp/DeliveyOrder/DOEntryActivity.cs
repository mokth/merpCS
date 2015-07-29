
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
using Android.Views.InputMethods;

namespace wincom.mobile.erp
{
	[Activity (Label = "DELIVERY ORDER ITEM ENTRY")]			
	public class DOEntryActivity : Activity,IEventListener
	{
		string pathToDatabase;
		List<Item> items = null;
		string EDITMODE ="";
		string CUSTOMER ="";
		string CUSTCODE ="";
		string ITEMUID ="";
		string DELIVERYNO="";
		string FIRSTLOAD="";
		string compCode;
		string branchCode;
		Spinner spinner;
		ArrayAdapter<String> dataAdapter;
	//	double taxper;
//		bool isInclusive;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}

			EventManagerFacade.Instance.GetEventManager().AddListener(this);
			pathToDatabase = ((GlobalvarsApp)this.Application).DATABASE_PATH;
			compCode = ((GlobalvarsApp)this.Application).COMPANY_CODE;
			branchCode = ((GlobalvarsApp)this.Application).BRANCH_CODE;

			DELIVERYNO = Intent.GetStringExtra ("invoiceno") ?? "AUTO";
			ITEMUID = Intent.GetStringExtra ("itemuid") ?? "AUTO";
			EDITMODE = Intent.GetStringExtra ("editmode") ?? "AUTO";
			CUSTOMER= Intent.GetStringExtra ("customer") ?? "AUTO";
			CUSTCODE= Intent.GetStringExtra ("custcode") ?? "AUTO";
			// Create your application here
			SetContentView (Resource.Layout.DOEntry);
			spinner = FindViewById<Spinner> (Resource.Id.txtcode);
			EditText qty = FindViewById<EditText> (Resource.Id.txtqty);

			TextView txtInvNo =  FindViewById<TextView> (Resource.Id.txtInvnp);
			TextView txtcust =  FindViewById<TextView> (Resource.Id.txtInvcust);
			Button butFind = FindViewById<Button> (Resource.Id.newinv_bfind);
			txtInvNo.Text = DELIVERYNO;
			txtcust.Text = CUSTOMER;
			Button but = FindViewById<Button> (Resource.Id.Save);
			Button but2 = FindViewById<Button> (Resource.Id.Cancel);
			spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs> (spinner_ItemSelected);
			but.Click += butSaveClick;
			but2.Click += butCancelClick;
			butFind.Click+= (object sender, EventArgs e) => {
				ShowItemLookUp();
			};
			qty.EditorAction += HandleEditorAction;
			qty.AfterTextChanged+= Qty_AfterTextChanged;


			//SqliteConnection.CreateFile(pathToDatabase);
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{
				items = db.Table<Item>().Where(x=>x.CompCode==compCode&&x.BranchCode==branchCode).ToList<Item> ();
			}

			List<string> icodes = new List<string> ();
			foreach (Item item in items) {
				if (item.IDesc.Length > 40) {
					icodes.Add (item.ICode + " | " + item.IDesc.Substring(0,40)+"...");
				}else icodes.Add (item.ICode + " | " + item.IDesc);
			}

			dataAdapter = new ArrayAdapter<String>(this,Resource.Layout.spinner_item, icodes);

			// Drop down layout style - list view with radio button
			dataAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);


			// attaching data adapter to spinner
			spinner.Adapter =dataAdapter;
			if (EDITMODE == "EDIT") {
				FIRSTLOAD="1";
				LoadData (DELIVERYNO, ITEMUID);
			}
		}

		void Qty_AfterTextChanged (object sender, Android.Text.AfterTextChangedEventArgs e)
		{
			//CalAmt ();
		}

		private void HandleEditorAction(object sender, TextView.EditorActionEventArgs e)
		{
			e.Handled = false;
			if ((e.ActionId == ImeAction.Done)||(e.ActionId == ImeAction.Next))
			{
				//CalAmt ();
				e.Handled = true;   
				//Button butSave = FindViewById<Button> (Resource.Id.Save);
				//butSave.RequestFocus ();
			}
		}


		private void LoadData(string sono,string uid)
		{
			TextView txtInvNo =  FindViewById<TextView> (Resource.Id.txtInvnp);
			Spinner spinner = FindViewById<Spinner> (Resource.Id.txtcode);
			EditText qty = FindViewById<EditText> (Resource.Id.txtqty);

			int id = Convert.ToInt32 (uid);

			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var invlist =db.Table<DelOrderDtls> ().Where (x => x.dono == sono&& x.ID==id).ToList<DelOrderDtls> ();
				if (invlist.Count > 0) {
					DelOrderDtls doItem = invlist [0];
					//int index = dataAdapter.GetPosition (doItem.icode + " | " + doItem.description);
					int index = -1;
					if (doItem.description.Length > 40)
						index = dataAdapter.GetPosition (doItem.icode + " | " + doItem.description.Substring (0, 40) + "...");
					else
						index = dataAdapter.GetPosition (doItem.icode + " | " + doItem.description);
					Item item =items.Where (x => x.ICode == doItem.icode).FirstOrDefault ();
					spinner.SetSelection (index);
					qty.Text = doItem.qty.ToString ();
				}
			}

		}

		private void butSaveClick(object sender,EventArgs e)
		{
			TextView txtInvNo =  FindViewById<TextView> (Resource.Id.txtInvnp);
			Spinner spinner = FindViewById<Spinner> (Resource.Id.txtcode);
			EditText qty = FindViewById<EditText> (Resource.Id.txtqty);
			if (spinner.SelectedItem == null) {
				Toast.MakeText (this, "No Item Code selected...", ToastLength.Long).Show ();			
				spinner.RequestFocus ();
				return;			
			}

			if (string.IsNullOrEmpty(qty.Text)) {
				Toast.MakeText (this, "Qty is blank...", ToastLength.Long).Show ();			
				qty.RequestFocus ();
				return;
			}

			double stqQty = Convert.ToDouble(qty.Text);


			DelOrderDtls doorder = new DelOrderDtls ();
			string[] codedesc = spinner.SelectedItem.ToString ().Split (new char[]{ '|' });
			doorder.dono = txtInvNo.Text;
			//doorder.description = codedesc [1].Trim();
			doorder.icode = codedesc [0].Trim();// spinner.SelectedItem.ToString ();
			doorder.qty = stqQty;
			doorder.CompCode = compCode;
			doorder.BranchCode = branchCode;

			var itemlist = items.Where (x => x.ICode == doorder.icode).ToList<Item> ();
			if (itemlist.Count == 0) {
				Toast.MakeText (this, "Invlaid Item Code...", ToastLength.Long).Show ();
				return;
			}

			Item ItemCode = itemlist [0];
			doorder.description = ItemCode.IDesc;

			int id = Convert.ToInt32 (ITEMUID);				
			//so..title = spinner.SelectedItem.ToString ();
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var invlist =db.Table<DelOrderDtls> ().Where (x => x.dono == doorder.dono&& x.ID==id).ToList<DelOrderDtls> ();
				if (invlist.Count > 0) {
					DelOrderDtls doItem = invlist [0];
					doItem.description = ItemCode.IDesc;//codedesc [1].Trim();
					doItem.icode =  codedesc [0].Trim(); //spinner.SelectedItem.ToString ();

					doItem.qty = stqQty;
					db.Update (doItem);
				}else db.Insert (doorder);
			}

			spinner.SetSelection (-1);
			qty.Text = "";
			Toast.MakeText (this, "Item successfully added...", ToastLength.Long).Show ();
		}
		public override void OnBackPressed() {
			// do nothing.
		}

		private void butCancelClick(object sender,EventArgs e)
		{
			string dono = Intent.GetStringExtra ("invoiceno") ?? "AUTO";
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var itemlist = db.Table<DelOrderDtls> ().Where (x => x.dono == dono&&x.CompCode==compCode&&x.BranchCode==branchCode);	
				double ttlamt= itemlist.Sum (x => x.qty);
				//double ttltax= itemlist.Sum (x => x.tax);
				var invlist =db.Table<DelOrder> ().Where (x => x.dono == dono&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<DelOrder> ();
				if (invlist.Count > 0) {
					invlist [0].amount = ttlamt;
					invlist [0].taxamt = 0;
					db.Update (invlist [0]);
				}
			}
			//base.OnBackPressed();
			var intent = new Intent(this, typeof(DOItemActivity));
			intent.PutExtra ("invoiceno",DELIVERYNO );
			intent.PutExtra ("custcode",CUSTCODE );
			StartActivity(intent);
		}
		private void spinner_ItemSelected (object sender, AdapterView.ItemSelectedEventArgs e)
		{
			Spinner spinner = (Spinner)sender;

			//string []codedesc = spinner.GetItemAtPosition (e.Position).ToString().Split (new char[]{ '|' });
			//string icode = codedesc[0].Trim();
			//Item item =items.Where (x => x.ICode == icode).FirstOrDefault ();
			//TextView tax =  FindViewById<TextView> (Resource.Id.txttax);
			//EditText price = FindViewById<EditText> (Resource.Id.txtprice);
			EditText qty = FindViewById<EditText> (Resource.Id.txtqty);
			qty.RequestFocus ();

		}

		void ShowItemLookUp()
		{
			var dialog = ItemDialog.NewInstance();
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
			case EventID.ICODE_SELECTED:
				RunOnUiThread (() => SetSelectedItem(e.Param["SELECTED"].ToString()));
				break;
			}
		}
	}
}

