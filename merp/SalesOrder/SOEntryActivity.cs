
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
	[Activity (Label = "SALES ORDER ITEM ENTRY")]			
	public class SOEntryActivity : Activity,IEventListener
	{
		string pathToDatabase;
		string compCode;
		string branchCode;
		List<Item> items = null;
		string EDITMODE ="";
		string CUSTOMER ="";
		string CUSTCODE ="";
		string ITEMUID ="";
		string SALEORDERNO="";
		string FIRSTLOAD="";
		Spinner spinner;
		ArrayAdapter<String> dataAdapter;
		double taxper;
		bool isInclusive;
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

			SALEORDERNO = Intent.GetStringExtra ("invoiceno") ?? "AUTO";
			ITEMUID = Intent.GetStringExtra ("itemuid") ?? "AUTO";
			EDITMODE = Intent.GetStringExtra ("editmode") ?? "AUTO";
			CUSTOMER= Intent.GetStringExtra ("customer") ?? "AUTO";
			CUSTCODE= Intent.GetStringExtra ("custcode") ?? "AUTO";
			// Create your application here
			SetContentView (Resource.Layout.SOEntry);
			spinner = FindViewById<Spinner> (Resource.Id.txtcode);
			EditText qty = FindViewById<EditText> (Resource.Id.txtqty);
			EditText price = FindViewById<EditText> (Resource.Id.txtprice);
			TextView txtInvNo =  FindViewById<TextView> (Resource.Id.txtInvnp);
			TextView txtcust =  FindViewById<TextView> (Resource.Id.txtInvcust);
			Button butFind = FindViewById<Button> (Resource.Id.newinv_bfind);
			txtInvNo.Text = SALEORDERNO;
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
			price.Enabled = false;
			//price.EditorAction += HandleEditorAction; 
		
			//SqliteConnection.CreateFile(pathToDatabase);
			using (var db = new SQLite.SQLiteConnection(pathToDatabase))
			{

				items = db.Table<Item> ().ToList<Item> ();
			}

			List<string> icodes = LoadItems ();

			dataAdapter = new ArrayAdapter<String>(this,Resource.Layout.spinner_item, icodes);

			// Drop down layout style - list view with radio button
			dataAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);


			// attaching data adapter to spinner
			spinner.Adapter =dataAdapter;
			if (EDITMODE == "EDIT") {
				FIRSTLOAD="1";
				LoadData (SALEORDERNO, ITEMUID);
			}
		}

		List<string> LoadItems ()
		{
			//SqliteConnection.CreateFile(pathToDatabase);
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				items = db.Table<Item> ().Where (x => x.CompCode == compCode && x.BranchCode == branchCode).ToList<Item> ();
			}
			List<string> icodes = new List<string> ();
			foreach (Item item in items) {
				//icodes.Add (item.ICode + " | " + item.IDesc);
				if (item.IDesc.Length > 40) {
					icodes.Add (item.ICode + " | " + item.IDesc.Substring(0,40)+"...");
				}else icodes.Add (item.ICode + " | " + item.IDesc);
			}
			return icodes;
		}

		void Qty_AfterTextChanged (object sender, Android.Text.AfterTextChangedEventArgs e)
		{
			CalAmt ();
		}

		private void HandleEditorAction(object sender, TextView.EditorActionEventArgs e)
		{
			e.Handled = false;
			if ((e.ActionId == ImeAction.Done)||(e.ActionId == ImeAction.Next))
			{
				CalAmt ();
				e.Handled = true;   
				//Button butSave = FindViewById<Button> (Resource.Id.Save);
				//butSave.RequestFocus ();
			}
		}
		private void CalAmt()
		{
			EditText ttlamt = FindViewById<EditText> (Resource.Id.txtamount);
			EditText ttltax = FindViewById<EditText> (Resource.Id.txttaxamt);
			EditText qty = FindViewById<EditText> (Resource.Id.txtqty);
			EditText price = FindViewById<EditText> (Resource.Id.txtprice);
			//EditText taxper = FindViewById<EditText> (Resource.Id.txtinvtaxper);
			//CheckBox isincl = FindViewById<CheckBox> (Resource.Id.txtinvisincl);
			TextView txttax =  FindViewById<TextView> (Resource.Id.txttax);
			try{
				double taxval = taxper;// Convert.ToDouble(taxper.Text);
			double stqQty = Convert.ToDouble(qty.Text);
			double uprice = Convert.ToDouble(price.Text);
			double amount = Math.Round((stqQty * uprice),2);
			double netamount = amount;
				bool taxinclusice =  isInclusive;// isincl.Checked;
			double taxamt = 0;
			if (taxinclusice) {
				double percent = (taxval/100) + 1;
				double amt2 =Math.Round( amount / percent,2,MidpointRounding.AwayFromZero);
				taxamt = amount - amt2;
				netamount = amount - taxamt;

			} else {
				taxamt = Math.Round(amount * (taxval / 100),2,MidpointRounding.AwayFromZero);
			}
			ttlamt.Text = netamount.ToString("n2");
			ttltax.Text = taxamt.ToString("n2");
			}catch{
			}
		}

		private void LoadData(string sono,string uid)
		{
			TextView txtInvNo =  FindViewById<TextView> (Resource.Id.txtInvnp);
			Spinner spinner = FindViewById<Spinner> (Resource.Id.txtcode);
			EditText qty = FindViewById<EditText> (Resource.Id.txtqty);
			EditText price = FindViewById<EditText> (Resource.Id.txtprice);
			EditText amount = FindViewById<EditText> (Resource.Id.txtamount);
			EditText taxamt = FindViewById<EditText> (Resource.Id.txttaxamt);
			TextView tax =  FindViewById<TextView> (Resource.Id.txttax);

			int id = Convert.ToInt32 (uid);

			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var invlist =db.Table<SaleOrderDtls> ().Where (x => x.sono == sono&& x.ID==id).ToList<SaleOrderDtls> ();
				if (invlist.Count > 0) {
					SaleOrderDtls soItem = invlist [0];
					//int index = dataAdapter.GetPosition (soItem.icode + " | " + soItem.description);
					int index = -1;
					if (soItem.description.Length > 40)
						index = dataAdapter.GetPosition (soItem.icode + " | " + soItem.description.Substring (0, 40) + "...");
					else
						index = dataAdapter.GetPosition (soItem.icode + " | " + soItem.description);
					
					Item item =items.Where (x => x.ICode == soItem.icode).FirstOrDefault ();
					spinner.SetSelection (index);
					qty.Text = soItem.qty.ToString ();
					price.Text = soItem.price.ToString ();
					taxamt.Text = soItem.tax.ToString ();

					tax.Text = item.taxgrp;
					taxper = item.tax;
					isInclusive = item.isincludesive;
					amount.Text = soItem.amount.ToString ();
					price.Text = soItem.price.ToString ();
				}
			}

		}

		private void butSaveClick(object sender,EventArgs e)
		{
			TextView txtInvNo =  FindViewById<TextView> (Resource.Id.txtInvnp);
			Spinner spinner = FindViewById<Spinner> (Resource.Id.txtcode);
			EditText qty = FindViewById<EditText> (Resource.Id.txtqty);
			EditText price = FindViewById<EditText> (Resource.Id.txtprice);
			TextView txttax =  FindViewById<TextView> (Resource.Id.txttax);
			EditText ttlamt = FindViewById<EditText> (Resource.Id.txtamount);
			EditText ttltax = FindViewById<EditText> (Resource.Id.txttaxamt);
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
			if (string.IsNullOrEmpty(price.Text)) {
				Toast.MakeText (this, "Unit Price is blank...", ToastLength.Long).Show ();			
				price.RequestFocus ();
				return;
			}
			double stqQty = Convert.ToDouble(qty.Text);
			double uprice = Convert.ToDouble(price.Text);
			double taxval = taxper;//Convert.ToDouble(taxper.Text);
			double amount = Math.Round((stqQty * uprice),2);
			double netamount = amount;
			bool taxinclusice = isInclusive;// isincl.Checked;
			double taxamt = 0;
			if (taxinclusice) {
				double percent = (taxval/100) + 1;
				double amt2 =Math.Round( amount / percent,2,MidpointRounding.AwayFromZero);
				taxamt = amount - amt2;
				netamount = amount - taxamt;
			
			} else {
				taxamt = Math.Round(amount * (taxval / 100),2,MidpointRounding.AwayFromZero);
			}

			SaleOrderDtls so = new SaleOrderDtls ();
			string[] codedesc = spinner.SelectedItem.ToString ().Split (new char[]{ '|' });
			so.sono = txtInvNo.Text;
			so.amount = amount;
			so.description = codedesc [1].Trim();
			so.icode = codedesc [0].Trim();// spinner.SelectedItem.ToString ();
			so.price = uprice;
			so.qty = stqQty;
			so.tax = taxamt;
			so.taxgrp = txttax.Text;
			so.netamount = netamount;

			var itemlist = items.Where (x => x.ICode == so.icode).ToList<Item> ();
			if (itemlist.Count == 0) {
				Toast.MakeText (this, "Invlaid Item Code...", ToastLength.Long).Show ();
				return;
			}
			Item ItemCode = itemlist [0];
			so.description = ItemCode.IDesc;

			int id = Convert.ToInt32 (ITEMUID);				
			//so..title = spinner.SelectedItem.ToString ();
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var invlist =db.Table<SaleOrderDtls> ().Where (x => x.sono == so.sono&& x.ID==id).ToList<SaleOrderDtls> ();
				if (invlist.Count > 0) {
					SaleOrderDtls soItem = invlist [0];
					soItem.amount = amount;
					soItem.netamount = netamount;
					soItem.tax = taxamt;
					soItem.taxgrp = txttax.Text;
					//soItem.description =  codedesc [1].Trim();
					soItem.description =  ItemCode.IDesc;
					soItem.icode =  codedesc [0].Trim(); //spinner.SelectedItem.ToString ();
					soItem.price = uprice;
					soItem.qty = stqQty;
					db.Update (soItem);
				}else db.Insert (so);
			}

			spinner.SetSelection (-1);
			qty.Text = "";
			//price.Text = "";
			ttltax.Text = "";
			ttlamt.Text = "";
			Toast.MakeText (this, "Item successfully added...", ToastLength.Long).Show ();
		}
		public override void OnBackPressed() {
			// do nothing.
		}

		private void butCancelClick(object sender,EventArgs e)
		{
			string invno = Intent.GetStringExtra ("invoiceno") ?? "AUTO";
			using (var db = new SQLite.SQLiteConnection (pathToDatabase)) {
				var itemlist = db.Table<SaleOrderDtls> ().Where (x => x.sono == invno&&x.CompCode==compCode&&x.BranchCode==branchCode);	
				double ttlamt= itemlist.Sum (x => x.netamount);
				double ttltax= itemlist.Sum (x => x.tax);
				var invlist =db.Table<SaleOrder> ().Where (x => x.sono == invno&&x.CompCode==compCode&&x.BranchCode==branchCode).ToList<SaleOrder> ();
				if (invlist.Count > 0) {
					invlist [0].amount = ttlamt;
					invlist [0].taxamt = ttltax;
					db.Update (invlist [0]);
				}
			}
			//base.OnBackPressed();
			var intent = new Intent(this, typeof(SOItemActivity));
			intent.PutExtra ("invoiceno",SALEORDERNO );
			intent.PutExtra ("custcode",CUSTCODE );
			StartActivity(intent);
		}
		private void spinner_ItemSelected (object sender, AdapterView.ItemSelectedEventArgs e)
		{
			Spinner spinner = (Spinner)sender;

			string []codedesc = spinner.GetItemAtPosition (e.Position).ToString().Split (new char[]{ '|' });
			string icode = codedesc[0].Trim();
			Item item =items.Where (x => x.ICode == icode).FirstOrDefault ();
			TextView tax =  FindViewById<TextView> (Resource.Id.txttax);
			EditText price = FindViewById<EditText> (Resource.Id.txtprice);
			EditText qty = FindViewById<EditText> (Resource.Id.txtqty);
		
			if (FIRSTLOAD=="")
				price.Text = item.Price.ToString ();
			else FIRSTLOAD="";
			tax.Text = item.taxgrp;
			taxper = item.tax;
			isInclusive = item.isincludesive;
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

