using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace JPFITS
{
	public class REG
	{
		public static void SetReg(string programName, string keyName, object keyValue)
		{
			RegistryKey User = Registry.CurrentUser;
			RegistryKey SW = User.OpenSubKey("Software", true);
			RegistryKey AstroWerks = SW.CreateSubKey("AstroWerks");
			RegistryKey SUBKEY = AstroWerks.CreateSubKey(programName);
			SUBKEY.SetValue(keyName, keyValue);
		}

		public static object GetReg(string programName, string keyName)
		{
			RegistryKey User = Registry.CurrentUser;
			RegistryKey SW = User.OpenSubKey("Software", true);
			RegistryKey AstroWerks = SW.CreateSubKey("AstroWerks");
			RegistryKey SUBKEY = AstroWerks.CreateSubKey(programName);
			return SUBKEY.GetValue(keyName); ;
		}
	}
}
