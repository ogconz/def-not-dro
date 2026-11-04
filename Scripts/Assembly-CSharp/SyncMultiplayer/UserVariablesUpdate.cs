using System.Collections.Generic;
using Sfs2X.Entities;

namespace SyncMultiplayer
{
	public delegate void UserVariablesUpdate(SmartfoxRoomClient client, SFSUser sender, HashSet<string> changedProps);
}
