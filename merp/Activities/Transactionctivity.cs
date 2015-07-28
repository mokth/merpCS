
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
	[Activity (Label = "TRANSACTIONS")]			
	public class TransactionsActivity : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			if (!((GlobalvarsApp)this.Application).ISLOGON) {
				Finish ();
			}
			SetContentView (Resource.Layout.Transactions);
			Button butInvlist = FindViewById<Button> (Resource.Id.butInvcash);
			butInvlist.Click+= ButInvlist_Click;

			Button butSOList = FindViewById<Button> (Resource.Id.butSO);
			butSOList.Click+= ButSOList_Click;

			Button butDOList = FindViewById<Button> (Resource.Id.butDO);
			butDOList.Click+= ButDOList_Click;

			Button butCNNoteList = FindViewById<Button> (Resource.Id.butCN);
			butCNNoteList.Click+= ButCNNoteList_Click;



			Button butback = FindViewById<Button> (Resource.Id.butMain);
			butback.Click+= (object sender, EventArgs e) => {
				StartActivity(typeof(MainActivity));
			};
		}

		void ButSOList_Click (object sender, EventArgs e)
		{
			var intent = new Intent(this, typeof(SalesOrderActivity));
			StartActivity(intent);

		}

		void ButInvlist_Click (object sender, EventArgs e)
		{
			var intent = new Intent(this, typeof(InvoiceActivity));
			StartActivity(intent);
		}

		void ButCNNoteList_Click (object sender, EventArgs e)
		{
			var intent = new Intent(this, typeof(CNNoteActivity));

			StartActivity(intent);
		}

		void ButDOList_Click (object sender, EventArgs e)
		{
			var intent = new Intent(this, typeof(DelOrderActivity));

			StartActivity(intent);
		}
	}
}

