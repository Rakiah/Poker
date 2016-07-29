using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Player
{
	public string pseudo;
	public int place;
	public int chips;
	public NetworkPlayer player;

	public bool isDisconnected;

	public bool isCurrentTurn;
	public bool isFeld;
	public int currentBet;

	public List<Transform> CardsPosition = new List<Transform>();
	public List<Text> UIInfos = new List<Text>();
	public Transform DealerPosition;
	public Transform FoldPosition;

	public List<Cards> cardsInHands = new List<Cards>();

	public Hand bestHand;

	public void Release()
	{
		UIInfos[0].text = "";
		UIInfos[1].text = "";
		UIInfos[2].text = "";
		UIInfos[3].enabled = false;
		UIInfos[4].enabled = false;

		Transform holder = GameObject.Find("Player " + place.ToString()).transform;
		holder.GetChild(3).gameObject.SetActive(false);
	}


	public Player (int _id, NetworkPlayer _player, string _pseudo, int _chips)
	{
		pseudo = _pseudo;
		player = _player;
		place = _id;
		chips = _chips;
		isDisconnected = false;
		
		Transform holder = GameObject.Find("Player " + place.ToString()).transform;
		
		CardsPosition.Add(holder.GetChild(0));
		CardsPosition.Add(holder.GetChild(1));
		DealerPosition = holder.GetChild(2);
		
		holder.GetChild(3).gameObject.SetActive(true);
		
		for(int i = 0; i < 5; i++) UIInfos.Add(holder.GetChild(3).GetChild(i).GetComponent<Text>());
	}
	

	public Player (int _id, NetworkPlayer _player, string _pseudo)
	{
		pseudo = _pseudo;
		player = _player;
		place = _id;
		isDisconnected = false;
		
		Transform holder = GameObject.Find("Player " + place.ToString()).transform;
		
		CardsPosition.Add(holder.GetChild(0));
		CardsPosition.Add(holder.GetChild(1));
		DealerPosition = holder.GetChild(2);

		holder.GetChild(3).gameObject.SetActive(true);

		for(int i = 0; i < 5; i++) UIInfos.Add(holder.GetChild(3).GetChild(i).GetComponent<Text>());
	}
	public Player(int _id, NetworkPlayer _player, int _chips)
	{
		player = _player;
		place = _id;
		pseudo = "";
		chips = _chips;
		isDisconnected = false;
		
		Transform holder = GameObject.Find("Player " + place.ToString()).transform;
		
		CardsPosition.Add(holder.GetChild(0));
		CardsPosition.Add(holder.GetChild(1));
		DealerPosition = holder.GetChild(2);

		holder.GetChild(3).gameObject.SetActive(true);

		for(int i = 0; i < 5; i++) UIInfos.Add(holder.GetChild(3).GetChild(i).GetComponent<Text>());
	}
	public Player (int _id, NetworkPlayer _player)
	{
		player = _player;
		place = _id;
		pseudo = "";
		isDisconnected = false;

		Transform holder = GameObject.Find("Player " + place.ToString()).transform;

		CardsPosition.Add(holder.GetChild(0));
		CardsPosition.Add(holder.GetChild(1));
		DealerPosition = holder.GetChild(2);

		holder.GetChild(3).gameObject.SetActive(true);

		for(int i = 0; i < 5; i++) UIInfos.Add(holder.GetChild(3).GetChild(i).GetComponent<Text>());
	}
	public Player(int _id)
	{
		place = _id;
		pseudo = "";
		isDisconnected = false;
		
		Transform holder = GameObject.Find("Player " + place.ToString()).transform;
		
		CardsPosition.Add(holder.GetChild(0));
		CardsPosition.Add(holder.GetChild(1));
		DealerPosition = holder.GetChild(2);

		holder.GetChild(3).gameObject.SetActive(true);

		for(int i = 0; i < 5; i++) UIInfos.Add(holder.GetChild(3).GetChild(i).GetComponent<Text>());
	}

	public Player(string name)
	{
		pseudo = name;
	}

	public void UpdateUI ()
	{
		UIInfos[0].text = pseudo;
		UIInfos[1].text = currentBet.ToString();
		UIInfos[2].text = chips.ToString();
		UIInfos[3].enabled = isCurrentTurn;
		UIInfos[4].enabled = isFeld;
	}

	public void AddCard (Cards c)
	{
		cardsInHands.Add(c);
	}

	public void RemoveCards ()
	{
		cardsInHands.Clear();
	}

	public void Fold ()
	{
		isFeld = true;
		isCurrentTurn = false;
	}

	public int CheckFollow (int maxBet)
	{
		if(currentBet < maxBet)
		{
			int dif = maxBet - currentBet;

			if(chips - dif <= 0) dif = chips;
			chips -= dif;
			currentBet += dif;

			isCurrentTurn = false;
			return dif;
		}

		isCurrentTurn = false;
		return 0;
	}

	public int Raise (int maxBet, int bigBlind)
	{
		//here we calculate the difference for the player to add minimum
		int dif = (maxBet - currentBet) + bigBlind;

		if(chips - dif <= 0) dif = chips;
		chips -= dif;
		currentBet += dif;

		isCurrentTurn = false;
		return dif;
	}

	public void SetBestHand (List<Hand> hands)
	{
		int bHand = 0;
		foreach(Hand h in hands)
		{
			if(h != null)
			{
				int hRank = h.GetRank();
				if(hRank > bHand){ bHand = hRank; bestHand = h; }
			}
		}
	}

	public void ReturnCards (bool toShow)
	{
		foreach(Cards c in cardsInHands) 
			CoroutinesLauncher.Coroutines.StartCoroutine(CoroutinesLauncher.Coroutines.TranslateQuaternion(c.CardObject, c.CardObject.transform.rotation, Quaternion.Euler(new Vector3(toShow == true ? 90 : -90,-90,-90)), CardsSplitter.toolsSplitter.partySpeed));
	}
}
