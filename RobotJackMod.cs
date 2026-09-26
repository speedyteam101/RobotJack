using RobotJack.Common.Titan;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack
{
	public class RobotJackMod : Mod
	{
		internal enum MessageType : byte
		{
			TitanSync,
			BuildMechanic, // a client used a Robot Assembly Kit
		}

		public override void Unload() {
			TitanRenderer.Unload();
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			MessageType type = (MessageType)reader.ReadByte();
			switch (type) {
				case MessageType.TitanSync:
					byte playerIndex = reader.ReadByte();
					TitanPlayer titan = Main.player[playerIndex].GetModPlayer<TitanPlayer>();
					titan.ReadState(reader);
					if (Main.netMode == NetmodeID.Server) {
						// Forward to everyone else.
						titan.SyncPlayer(-1, whoAmI, false);
					}
					break;
				case MessageType.BuildMechanic:
					if (Main.netMode == NetmodeID.Server) {
						Common.Akutoku.AkutokuSystem.mechanicBuilt = true;
						NetMessage.SendData(MessageID.WorldData);
					}
					break;
			}
		}
	}
}
