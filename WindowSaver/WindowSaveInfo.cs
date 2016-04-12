using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace WindowSaver
{
	[Serializable]
	public class WindowSaveInfo
	{
		public Dictionary<IntPtr, WindowPosition> WindowSave { get; set; }
		public List<IntPtr> IterOrder { get; set; }
		public IntPtr ForegroundWindow { get; set; }

		public WindowSaveInfo()
		{
			this.WindowSave = new Dictionary<IntPtr, WindowPosition>();
			this.IterOrder = null;
			this.ForegroundWindow = IntPtr.Zero;
		}

		public void Save(string file)
		{
			using (FileStream fs = new FileStream(file, FileMode.Create))
			{
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(fs, this);
			}
		}

		public static WindowSaveInfo FromFile(string file)
		{
			WindowSaveInfo wsi;
			using (FileStream fs = new FileStream(file, FileMode.Open))
			{
				BinaryFormatter bf = new BinaryFormatter();
				wsi = (WindowSaveInfo)bf.Deserialize(fs);
			}
			return wsi;
		}
	}
}
