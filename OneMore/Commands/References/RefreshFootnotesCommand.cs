﻿//************************************************************************************************
// Copyright © 2021 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using System.Threading.Tasks;


	/// <summary>
	/// Resequences the bottom-of-page footnotes in the order in which the reference
	/// labels appear on the page
	/// </summary>
	[CommandService]
	internal class RefreshFootnotesCommand : Command
	{

		public RefreshFootnotesCommand()
		{
		}


		public override async Task Execute(params object[] args)
		{
			await using var one = new OneNote();
			var editor = new FootnoteEditor(one);
			if (editor.ValidContext())
			{
				await editor.RefreshLabels(true);
			}
		}
	}
}
