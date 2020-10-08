﻿//************************************************************************************************
// Copyright © 2016 Steven M Cohn.  Yada yada...
//************************************************************************************************

#pragma warning disable CS3001  // Type is not CLS-compliant

namespace River.OneMoreAddIn
{
	using Microsoft.Office.Interop.OneNote;
	using System;
	using System.Drawing;
	using System.Linq;
	using System.Windows.Forms;
	using System.Xml.Linq;


	/// <summary>
	/// View page and hierarchy XML. Update page XML if desired.
	/// </summary>
	/// <remarks>
	/// Disposables: Local ApplicationManager disposed in OnClosed.
	/// </remarks>

	internal partial class XmlDialog : Form, IOneMoreWindow
	{

		private ApplicationManager manager;
		private readonly ILogger logger;


		public XmlDialog()
		{
			InitializeComponent();

			if (DesignMode)
			{
				AutoScaleDimensions = new SizeF(96f, 96f);
				AutoScaleMode = AutoScaleMode.Dpi;
				Logger.SetDesignMode(DesignMode);
			}

			logger = Logger.Current;

			Width = Math.Min(2000, (int)(Screen.PrimaryScreen.WorkingArea.Width * 0.8));
			Height = Math.Min(1500, (int)(Screen.PrimaryScreen.WorkingArea.Height * 0.8));
		}

		#region Lifecycle

		private void MainForm_Load(object sender, EventArgs e)
		{
			manager = new ApplicationManager();

			var infoNames = Enum.GetNames(typeof(PageInfo));
			pageInfoBox.Items.AddRange(infoNames);
			pageInfoBox.SelectedIndex = infoNames.ToList().IndexOf("piSelection");

			var info = manager.GetCurrentPageInfo();
			pageName.Text = info.Name;
			pagePath.Text = info.Path;
			pageLink.Text = info.Link;
		}

		private void ChangeInfoScope(object sender, EventArgs e)
		{
			if (Enum.TryParse<PageInfo>(pageInfoBox.Text, out var info))
			{
				var page = manager.CurrentPage(info);
				if (page != null)
				{
					var xml = page.ToString(SaveOptions.None);
					pageBox.Text = xml;

					logger.WriteLine("XmlDialog loaded page, " + xml.Length + " chars");
				}
			}
		}


		protected override void OnShown(EventArgs e)
		{
			//Location = new System.Drawing.Point(30, 30);
			UIHelper.SetForegroundWindow(this);
			findBox.Focus();
		}


		private void Close(object sender, EventArgs e)
		{
			Close();
		}


		private void ChangeSelectedTab(object sender, EventArgs e)
		{
			if (tabs.SelectedIndex == 0)
			{
				pageBox.Select(0, 0);
				pageBox.Focus();
				updateButton.Visible = true;
				pageInfoPanel.Visible = true;
				wrapBox.Checked = pageBox.WordWrap;
			}
			else
			{
				if (hierBox.TextLength == 0)
				{
					ShowHierarchy(HierarchyScope.hsNotebooks);
				}

				pageBox.Select(0, 0);
				pageBox.Focus();
				updateButton.Visible = false;
				pageInfoPanel.Visible = false;
				wrapBox.Checked = hierBox.WordWrap;
			}
		}

		private void ChangeWrap(object sender, EventArgs e)
		{
			if (tabs.SelectedIndex == 0)
			{
				pageBox.WordWrap = wrapBox.Checked;
			}
			else
			{
				hierBox.WordWrap = wrapBox.Checked;
			}
		}


		private void HideAttributes(object sender, EventArgs e)
		{
			ChangeInfoScope(sender, e);

			if (!hideBox.Checked && !hideLFBox.Checked)
			{
				return;
			}

			var root = XElement.Parse(pageBox.Text);

			if (hideBox.Checked)
			{
				// EditedByAttributes and others
				root.Descendants().Attributes().Where(a =>
					a.Name.LocalName == "author"
					|| a.Name.LocalName == "authorInitials"
					|| a.Name.LocalName == "authorResolutionID"
					|| a.Name.LocalName == "lastModifiedBy"
					|| a.Name.LocalName == "lastModifiedByInitials"
					|| a.Name.LocalName == "lastModifiedByResolutionID"
					|| a.Name.LocalName == "creationTime"
					|| a.Name.LocalName == "lastModifiedTime"
					|| a.Name.LocalName == "objectID")
					.Remove();
			}

			if (hideLFBox.Checked)
			{
				var nodes = root.DescendantNodes().OfType<XCData>();
				if (!nodes.IsNullOrEmpty())
				{
					foreach (var cdata in nodes)
					{
						cdata.Value = cdata.Value
							.Replace("\nstyle", " style")
							.Replace("\nhref", " href")
							.Replace(";\nfont-size:", ";font-size:")
							.Replace(";\ncolor:", ";color:")
							.Replace(":\n", ": ");
					}
				}
			}

			pageBox.Text = root.ToString(SaveOptions.None);
		}


		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (keyData == (Keys.F | Keys.Control))
			{
				findBox.SelectAll();
				findBox.Focus();
				return true;
			}

			return base.ProcessDialogKey(keyData);
		}

		#endregion Lifecycle

		#region Selection action

		private void SelectAll(object sender, EventArgs e)
		{
			if (tabs.SelectedIndex == 0)
			{
				pageBox.SelectAll();
				pageBox.Focus();
			}
			else
			{
				hierBox.SelectAll();
				hierBox.Focus();
			}
		}

		#endregion Selection action

		#region Find actions

		int findIndex = -1;
		private void ClickFind(object sender, EventArgs e)
		{
			RichTextBox box = tabs.SelectedIndex == 0 ? pageBox : hierBox;
			var index = SearchOne(box, findBox.Text);
			if ((index < 0) && (findIndex > 0))
			{
				findIndex = -1;
				SearchOne(box, findBox.Text);
			}
		}

		private int SearchOne(RichTextBox box, string text)
		{
			var index = box.Find(text, findIndex + 1, RichTextBoxFinds.None);
			if (index > findIndex)
			{
				box.Select(index, findBox.Text.Length);
				box.Focus();
				findIndex = index;
			}

			return index;
		}

		private void ChangeFindText(object sender, EventArgs e)
		{
			if (findBox.Text.Length == 0)
			{
				findIndex = -1;
				findButton.Enabled = false;
			}
			else
			{
				if (findButton.Enabled)
				{
					findIndex = -1;
					findButton.Enabled = true;
				}
			}
		}

		private void FindBoxKeyUP(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				ClickFind(sender, e);
				e.Handled = true;
			}
		}

		private void PageBoxKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Control && (e.KeyCode == Keys.F))
			{
				findBox.Focus();
			}
		}

		#endregion Find actions

		#region Hierarchy actions

		private void ShowNotebooks(object sender, EventArgs e)
		{
			ShowHierarchy(HierarchyScope.hsNotebooks);
		}

		private void ShowSections(object sender, EventArgs e)
		{
			ShowHierarchy(HierarchyScope.hsSections);
		}

		private void ShowPages(object sender, EventArgs e)
		{
			ShowHierarchy(HierarchyScope.hsPages);
		}

		private void ShowCurrentNotebook(object sender, EventArgs e)
		{
			var element = manager.CurrentNotebook();
			if (element != null)
			{
				var xml = element.ToString(SaveOptions.None);
				hierBox.Text = xml;
			}
			else
			{
				hierBox.Text = "Cannot get current notebook hierarchy";
			}
		}

		private void ShowCurrentSection(object sender, EventArgs e)
		{
			var element = manager.CurrentSection();
			if (element != null)
			{
				var xml = element.ToString(SaveOptions.None);
				hierBox.Text = xml;
			}
			else
			{
				hierBox.Text = "Cannot get current section hierarchy";
			}
		}


		private void ShowHierarchy(HierarchyScope scope)
		{
			var element = manager.GetHierarchy(scope);
			if (element != null)
			{
				var xml = element.ToString(SaveOptions.None);
				hierBox.Text = xml;
			}
			else
			{
				hierBox.Text = $"Cannot get hierarchy for {scope}";
			}
		}

		#endregion Hierarchy actions


		private void Update(object sender, EventArgs e)
		{
			var result = MessageBox.Show(
				"Are you sure? This may corrupt the current page.",
				"Feelin lucky punk?",
				MessageBoxButtons.OKCancel,
				MessageBoxIcon.Warning);

			if (result == DialogResult.OK)
			{
				try
				{
					var page = XElement.Parse(pageBox.Text);
					manager.UpdatePageContent(page);
					Close();
				}
				catch (Exception exc)
				{
					logger.WriteLine("Error updating page content", exc);
				}
			}
		}
	}
}
