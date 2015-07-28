using System;
using System.Text.RegularExpressions;
using System.Text;

namespace wincom.mobile.erp
{
	public enum CardType
	{
		Unknown = 0,
		MasterCard = 1,
		VISA = 2,
		Amex = 3,
		Discover = 4,
		DinersClub = 5,
		JCB = 6,
		enRoute = 7
	}



	public class CCardHelper
	{
		// Class to hold credit card type information
		private class CardTypeInfo
		{
			public CardTypeInfo(string regEx, int length, CardType type)
			{
				RegEx = regEx;
				Length = length;
				Type = type;
			}

			public string RegEx { get; set; }
			public int Length { get; set; }
			public CardType Type { get; set; }
		}

		// Array of CardTypeInfo objects.
		// Used by GetCardType() to identify credit card types.
		private static CardTypeInfo[] _cardTypeInfo =
		{
			new CardTypeInfo("^(51|52|53|54|55)", 16, CardType.MasterCard),
			new CardTypeInfo("^(4)", 16, CardType.VISA),
			new CardTypeInfo("^(4)", 13, CardType.VISA),
			new CardTypeInfo("^(34|37)", 15, CardType.Amex),
			new CardTypeInfo("^(6011)", 16, CardType.Discover),
			new CardTypeInfo("^(300|301|302|303|304|305|36|38)", 
				14, CardType.DinersClub),
			new CardTypeInfo("^(3)", 16, CardType.JCB),
			new CardTypeInfo("^(2131|1800)", 15, CardType.JCB),
			new CardTypeInfo("^(2014|2149)", 15, CardType.enRoute),
		};

		public static CardType GetCardType(string cardNumber)
		{
			foreach (CardTypeInfo info in _cardTypeInfo)
			{
				if (cardNumber.Length == info.Length && 
					Regex.IsMatch(cardNumber, info.RegEx))
					return info.Type;
			}

			return CardType.Unknown;
		}

		public static bool IsCardNumberValid(string cardNumber)
		{
			int i, checkSum = 0;

			// Compute checksum of every other digit starting from right-most digit
			for (i = cardNumber.Length - 1; i >= 0; i -= 2)
				checkSum += (cardNumber[i] - '0');

			// Now take digits not included in first checksum, multiple by two,
			// and compute checksum of resulting digits
			for (i = cardNumber.Length - 2; i >= 0; i -= 2)
			{
				int val = ((cardNumber[i] - '0') * 2);
				while (val > 0)
				{
					checkSum += (val % 10);
					val /= 10;
				}
			}

			// Number is valid if sum of both checksums MOD 10 equals 0
			return ((checkSum % 10) == 0);
		}

		public static string NormalizeCardNumber(string cardNumber)
		{
			if (cardNumber == null)
				cardNumber = String.Empty;

			StringBuilder sb = new StringBuilder();

			foreach (char c in cardNumber)
			{
				if (Char.IsDigit(c))
					sb.Append(c);
			}

			return sb.ToString();
		}
	}
}

