using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace RobotJack.Common.Akutoku
{
	// World flags for Akutoku-ō: whether he's been beaten, and whether the Robo-Mechanic has been built
	// (with a Robot Assembly Kit) so it can move into a house.
	public class AkutokuSystem : ModSystem
	{
		public static bool downedAkutoku;
		public static bool mechanicBuilt;

		public override void ClearWorld() {
			downedAkutoku = false;
			mechanicBuilt = false;
		}

		public override void SaveWorldData(TagCompound tag) {
			tag["downedAkutoku"] = downedAkutoku;
			tag["mechanicBuilt"] = mechanicBuilt;
		}

		public override void LoadWorldData(TagCompound tag) {
			downedAkutoku = tag.GetBool("downedAkutoku");
			mechanicBuilt = tag.GetBool("mechanicBuilt");
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(downedAkutoku);
			writer.Write(mechanicBuilt);
		}

		public override void NetReceive(BinaryReader reader) {
			downedAkutoku = reader.ReadBoolean();
			mechanicBuilt = reader.ReadBoolean();
		}

		public override void Unload() {
			SpiderRig.Unload();
		}
	}
}
