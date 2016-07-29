using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Cards : IComparable<Cards>
{
	public GameObject CardObject;
	public Texture2D texture;
	public Type type;
	public int Rank;

	public Cards () {}

	public Cards (GameObject obj, Texture2D tex, int _rank, Type _type)
	{
		CardObject = obj;
		texture = tex;
		Rank = _rank;
		type = _type;

		CardObject.renderer.material.mainTexture = texture;
	}

	public override string ToString ()
	{
		return CardsSplitter.toolsSplitter.rankToCard(Rank) + " of " + type.ToString();
	}

	#region IComparable<Cards> Members
	public int CompareTo (Cards other)
	{
		if(this.Rank < other.Rank) return 1;
		else if(this.Rank > other.Rank) return -1;
		else return 0;
	}
	#endregion
}

[System.Serializable]
public class TexturePackCards
{
	public string name;
	public Texture2D TexturePack;
	public Rect CardParser = new Rect(217, 222, 138, 200);
	public Vector2 SpriteLength = new Vector2(5, 4);

	public List<Cards> LCards = new List<Cards>();
}

[System.Serializable]
public class CenterCards
{
	public Transform CardPosition;
	public Cards card;
}

public enum Type { Carreau, Pique, Coeur, Trèfle };
