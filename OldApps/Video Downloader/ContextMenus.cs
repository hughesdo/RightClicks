using System;
using System.Diagnostics;
using System.Windows.Forms;
using SystemTrayApp.Properties;
using System.Drawing;

namespace SystemTrayApp
{
	/// <summary>
	/// 
	/// </summary>
	class ContextMenus
	{
		/// <summary>
		/// Is the About box displayed?
		/// </summary>
		bool isAboutLoaded = false;

		/// <summary>
		/// Creates this instance.
		/// </summary>
		/// <returns>ContextMenuStrip</returns>
		public ContextMenuStrip Create()
		{
			// Add the default menu options.
			ContextMenuStrip menu = new ContextMenuStrip();
			ToolStripMenuItem item;
			ToolStripSeparator sep;

			// Windows Explorer.
			item = new ToolStripMenuItem();
			item.Text = "Explorer";
			item.Click += new EventHandler(Explorer_Click);
			item.Image = Resources.Explorer;
			menu.Items.Add(item);

			// About.
			item = new ToolStripMenuItem();
			item.Text = "About";
			item.Click += new EventHandler(About_Click);
			item.Image = Resources.About;
			menu.Items.Add(item);

			// Separator.
			sep = new ToolStripSeparator();
			menu.Items.Add(sep);

			// Exit.
			item = new ToolStripMenuItem();
			item.Text = "Exit";
			item.Click += new System.EventHandler(Exit_Click);
			item.Image = Resources.Exit;
			menu.Items.Add(item);

			return menu;
		}

		/// <summary>
		/// Handles the Click event of the Explorer control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void Explorer_Click(object sender, EventArgs e)
		{
			// Open today's Memes folder
			OpenTodaysMemeFolder();
		}

		/// <summary>
		/// Opens today's Memes folder in Windows Explorer
		/// </summary>
		private void OpenTodaysMemeFolder()
		{
			try
			{
				string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
				string memesFolderPath = System.IO.Path.Combine(documentsPath, $"Memes_{currentDate}");

				// Check if today's folder exists
				if (System.IO.Directory.Exists(memesFolderPath))
				{
					// Open the folder
					Process.Start("explorer.exe", memesFolderPath);
				}
				else
				{
					// Folder doesn't exist yet, create it and open
					System.IO.Directory.CreateDirectory(memesFolderPath);
					Process.Start("explorer.exe", memesFolderPath);
				}
			}
			catch
			{
				// Fallback to opening Documents folder
				try
				{
					string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
					Process.Start("explorer.exe", documentsPath);
				}
				catch
				{
					// Last resort - just open Explorer
					Process.Start("explorer.exe");
				}
			}
		}

		/// <summary>
		/// Handles the Click event of the About control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void About_Click(object sender, EventArgs e)
		{
			if (!isAboutLoaded)
			{
				isAboutLoaded = true;
				new AboutBox().ShowDialog();
				isAboutLoaded = false;
			}
		}

		/// <summary>
		/// Processes a menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void Exit_Click(object sender, EventArgs e)
		{
			// Quit without further ado.
			Application.Exit();
		}
	}
}