using UnityEngine;

[CreateAssetMenu(fileName = "BlockDataScriptable", menuName = "Read Me/Create Block Info")]
public class BlockDataScriptable : ScriptableObject
{
	public string title; //1 or 2 words
	[TextArea]
	public string subTitle; //1 line
	public bool darkenBackground;
	public Sprite backgroundImage;
	public string resourceURL;
	[Tooltip("Size of the block, 1x1, 1x2.. a converter will change to pixel size")]
	public Vector2Int blockSize = new(1, 1);

}
