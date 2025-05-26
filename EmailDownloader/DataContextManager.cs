using System;
using System.Xml.Linq;
using System.Threading;
using System.Configuration;

namespace EmailDownloader
{
	public class DataContextManager
	{
		[ThreadStaticAttribute]
        private static DataClassesDataContext _Context;

        public static DataClassesDataContext Context
		{
			get
			{
				if (DataContextManager._Context == null)
				{
					DataContextManager._Context = new DataClassesDataContext(ConfigurationManager.ConnectionStrings["EmailDownloader.Properties.Settings.ActiveConnectionString"].ConnectionString);
					DataContextManager._Context.CommandTimeout = 3600;
				}
				return DataContextManager._Context;
			}
		}

		public static void RefreshNew()
		{
			if (DataContextManager._Context != null)
			{
				DataContextManager._Context.Dispose();
				DataContextManager._Context = null;
			}

			DataContextManager._Context = new DataClassesDataContext(ConfigurationManager.ConnectionStrings["EmailDownloader.Properties.Settings.ActiveConnectionString"].ConnectionString);
		}

		public static DataClassesDataContext GetNewContext()
		{
			return new DataClassesDataContext(ConfigurationManager.ConnectionStrings["EmailDownloader.Properties.Settings.ActiveConnectionString"].ConnectionString);
		}
	}
}
