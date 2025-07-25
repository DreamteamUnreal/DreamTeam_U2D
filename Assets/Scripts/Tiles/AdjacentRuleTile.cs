using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace HappyHarvest
{
	/// <summary>
	/// This special tile add a third rule : adjacent tiles, so you can match rules with other tile than this one.
	/// See the cliff tile for an example.
	/// </summary>
	[CreateAssetMenu]
	public class AdjacentRuleTile : RuleTile<AdjacentRuleTile.Neighbor>
	{
		public class Neighbor : TilingRuleOutput.Neighbor
		{
			public const int Adjacent = 3;
		}

		public TileBase[] AdjacentTiles;

		public override bool RuleMatch(int neighbor, TileBase other)
		{

			return neighbor switch
			{
				Neighbor.Adjacent => AdjacentTiles.Contains(other),
				_ => base.RuleMatch(neighbor, other),
			};
		}
	}
}