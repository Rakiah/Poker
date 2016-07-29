using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CardsSplitter : MonoBehaviour 
{
	public static CardsSplitter toolsSplitter;
	public ServerCardsDealer dealer;
	public GameObject Primitive;
	public Texture2D fakeTexture;
	public List<TexturePackCards> CardsPacks = new List<TexturePackCards>();
	public List<Cards> allCards = new List<Cards>();

	public List<int> totalHandsProba = new List<int>();

	public float partySpeed = 2.0f;
	public float timePerTurn = 30.0f;

	Vector3 defaultCardScale = new Vector3(0.55f, 1.0f, 0.004f);
	public float CardTall = 1.0f;

	// Use this for initialization
	void Awake () 
	{
		toolsSplitter = this;

		for(int i = 0; i < 10; i++) totalHandsProba.Add(0);

		Primitive.transform.localScale = (defaultCardScale * CardTall);
	}
	
	public void addHand (HandType type)
	{
		totalHandsProba[((int)type - 1)] ++;
	}


	public Texture2D SpriteToTexture2D(Sprite sp)
	{
		Texture2D Tx = new Texture2D((int)sp.rect.width, (int)sp.rect.height );
		
		Color[] pixels = sp.texture.GetPixels((int)sp.textureRect.x, 
		                                      (int)sp.textureRect.y, 
		                                      (int)sp.textureRect.width, 
		                                      (int)sp.textureRect.height);
		
		Tx.SetPixels(pixels);
		Tx.Apply();

		return Tx;
	}

	public Texture2D SplitTexture2D(Texture2D Original, int x, int y, int width, int height)
	{
		Texture2D Tx = new Texture2D(width, height);
		Color [] pixels = Original.GetPixels(x, y, width, height);
		Tx.SetPixels(pixels);
		Tx.Apply();

		return Tx;
	}

	public List<Cards> MergeHands (List<Cards> First, List<CenterCards> Second)
	{
		List<Cards> MergedHand = new List<Cards>();
		foreach(Cards F in First) MergedHand.Add(F);
		foreach(CenterCards S in Second) MergedHand.Add(S.card);

		return MergedHand;
	}

	public Cards FindCard (int rank, Type type)
	{
		foreach(Cards c in allCards) if(c.Rank == rank && c.type == type) return c;

		return null;
	}

	public string rankToCard (int rank)
	{
		switch (rank)
		{
			case 2 : return "Two";
			case 3 : return "Three";
			case 4 : return "Four";
			case 5 : return "Five";
			case 6 : return "Six";
			case 7 : return "Seven";
			case 8 : return "Eight";
			case 9 : return "Nine";
			case 10 : return "Ten";
			case 11 : return "Jack";
			case 12 : return "Queen";
			case 13 : return "King";
			case 14 : return "Ace";

			default : return "error";
		}
	}
}
