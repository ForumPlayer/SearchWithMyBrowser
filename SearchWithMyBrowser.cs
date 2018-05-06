using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SearchWithMyBrowser
{
	class Edge2Browser
	{
		static void LaunchInternetURL(string URL, bool BypassCatch = false)
		{
			try
			{
                var bing = @"https://www.bing.com";
                 if (URL.StartsWith(bing))
                {
                    URL = URL.Substring(bing.Length);
                    URL = @"https://www.google.com" + URL;
                }
				Process.Start(new ProcessStartInfo(){
					FileName = URL,
					UseShellExecute = true
				});
			}
			catch (Win32Exception exc)
			{
				if (exc.ErrorCode == -2147467259 && !BypassCatch) // https://support.microsoft.com/en-us/help/305703/how-to-start-the-default-internet-browser-programmatically-by-using-vi
				{
					var repairResult = MessageBox.Show(
						"It seems like your default browser is misconfigured.\n\nDo you want to open the Settings app to repair your default browser?",
						"SearchWithMyBrowser",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Exclamation
					);

					if (repairResult == DialogResult.Yes)
					{
						LaunchInternetURL("ms-settings:defaultapps", true); // Recursion!
					}
					else
					{
						var fallbackResult = MessageBox.Show(
							"Do you want to fallback to using Internet Explorer?",
							"SearchWithMyBrowser",
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question
						);

						if (fallbackResult == DialogResult.Yes)
						{
							Process.Start("iexplore.exe", URL);
						}
					}
					return;
				}
				throw;
			}
		}

		static void Main(string[] CommandLine)
		{
			if (CommandLine.Length == 0)
            {
                RegistryKey Software = Registry.LocalMachine.OpenSubKey("Software", true);
                RegistryKey Class = Software.CreateSubKey(@"Classes\SearchWithMyBrowser", true);
                Class.SetValue("URL Protocol", string.Empty);
                Class.SetValue(string.Empty, "URL:SearchWithMyBrowser Protocol");
                RegistryKey Command = Class.CreateSubKey(@"shell\open\command");
                Command.SetValue(string.Empty, "\"" + Process.GetCurrentProcess().MainModule.FileName + "\" \"%1\"");
                RegistryKey URLAssociations = Software.CreateSubKey(@"SearchWithMyBrowser\Capabilities\URLAssociations", true);
                URLAssociations.SetValue("microsoft-edge", "SearchWithMyBrowser");
                RegistryKey RegisteredApplications = Software.OpenSubKey("RegisteredApplications", true);
                RegisteredApplications.SetValue("SearchWithMyBrowser", @"Software\SearchWithMyBrowser\Capabilities");
                Process.Start("rundll32.exe", "shell32.dll,OpenAs_RunDLL microsoft-edge:");
            }
            else if (CommandLine[0] == "/unregister")
            {
                RegistryKey Software = Registry.LocalMachine.OpenSubKey("Software", true);
                RegistryKey Classes = Software.OpenSubKey("Classes", true);
                RegistryKey RegisteredApplications = Software.OpenSubKey("RegisteredApplications", true);
                RegisteredApplications.DeleteValue("SearchWithMyBrowser");
                Classes.DeleteSubKeyTree("SearchWithMyBrowser");
                Software.DeleteSubKeyTree("SearchWithMyBrowser");
                Classes.DeleteSubKeyTree("SearchWithMyBrowser");
            }
            else if (CommandLine[0].StartsWith("microsoft-edge:", StringComparison.OrdinalIgnoreCase))
			{
				string LaunchURL = CommandLine[0].Substring(15); // Remove "microsoft-edge:"

				if (LaunchURL.StartsWith("?launchContext1=", StringComparison.OrdinalIgnoreCase)) // Handle FCU
				{
					var ProtocolParameters = HttpUtility.ParseQueryString(LaunchURL);
					LaunchURL = ProtocolParameters["url"];
				}

				if (LaunchURL.StartsWith("//"))
					LaunchURL = LaunchURL.Substring(2);

				if (!new string[] {"http://", "https://"}.Any(ValidProtocol => LaunchURL.StartsWith(ValidProtocol, StringComparison.OrdinalIgnoreCase)))
					LaunchURL = "http://" + LaunchURL; // If there isn't a valid URL prefix, add one to prevent launching an arbitrary exe. (Or someone calling the protocol like this: "microsoft-edge:google.com")

				Uri url;
				if (Uri.TryCreate(LaunchURL, UriKind.Absolute, out url))
					LaunchInternetURL(url.AbsoluteUri);
				else
					throw new UriFormatException(); // When this happens, we can get a memory dump by WER containing the CommandLine array for further analysis and bugfixing.
			}
		}
	}
}