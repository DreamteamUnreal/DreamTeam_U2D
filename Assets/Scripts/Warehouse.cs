namespace HappyHarvest
{
	public class Warehouse : InteractiveObject
	{
		public override void InteractedWith()
		{
			UIHandler.OpenWarehouse();
		}
	}
}
