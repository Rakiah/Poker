using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Client : MonoBehaviour 
{
	int myID;
	public Player selfPlayer;
	public HUD hud;

	public ServerCardsDealer dealer;

	public List<string> pseudoRandom = new List<string>() {"Josh", "Patrick", "Elise", "Rakiah", "Pepeanuts" };

	void Start () 
	{
		dealer = CardsSplitter.toolsSplitter.dealer;
	}

	//giving informations to server
	
	public void ChooseRaise () { CardsSplitter.toolsSplitter.networkView.RPC("GetRaiseCall", RPCMode.Server); }
	
	public void ChooseCheckFollow () { CardsSplitter.toolsSplitter.networkView.RPC("GetCheckFollowCall", RPCMode.Server); }
	
	public void ChooseFold () { CardsSplitter.toolsSplitter.networkView.RPC("GetFoldCall", RPCMode.Server); }




	[RPC]
	public void DeletePlayer(int id)
	{
		Player p = dealer.system.Players[id];
		p.Release();
		dealer.system.Players.Remove(p);
	}


	//getting informations from server

	[RPC]
	public void GetInformations (int id, int place, int chips, string pseudo, NetworkPlayer play)
	{
		ServerSystem system = dealer.system;

		if(id > system.Players.Count - 1)
		{
			Player TempStats = new Player(place, play, pseudo, chips);
			system.Players.Add(TempStats);
		}
		else
		{
			system.Players[id].place = place;
			system.Players[id].pseudo = pseudo;
			system.Players[id].player = play;
			system.Players[id].chips = chips;
		}
		
		if(play == Network.player)
		{
			myID = id;
			selfPlayer = system.Players[id];
			if (pseudo == "")
			{
				selfPlayer.pseudo = hud.NameText.text;
				if (selfPlayer.pseudo == "")
					selfPlayer.pseudo = pseudoRandom[Random.Range(0,pseudoRandom.Count)];
				CardsSplitter.toolsSplitter.networkView.RPC("SetClientInformations", RPCMode.Server, myID, selfPlayer.pseudo);
			}
		}

		system.Players[id].UpdateUI();
	}

	[RPC]
	public void SetBet(int playerID, int bet)
	{
		dealer.system.Players[playerID].chips -= bet;
		dealer.system.Players[playerID].currentBet += bet;

		if(dealer.maxBet < dealer.system.Players[playerID].currentBet) dealer.maxBet = dealer.system.Players[playerID].currentBet;
		dealer.system.Players[playerID].UpdateUI();
	}

	[RPC]
	public void SetChips (int playerID, int chips)
	{
		dealer.system.Players[playerID].chips += chips;

		dealer.system.Players[playerID].UpdateUI();
	}

	[RPC]
	public void SetFold (int playerID)
	{
		dealer.system.Players[playerID].isFeld = true;
		dealer.system.Players[playerID].UpdateUI();
	}

	[RPC]
	public void SetTextUI(string str, Vector3 position, float timer)
	{
		if(timer > 0) StartCoroutine(hud.SetTextUI(str, position, timer));
		else hud.SetTextUI(str, position);
	}

	[RPC]
	public void AddPot(int playerID)
	{
		dealer.currentPot += dealer.system.Players[playerID].currentBet;
		dealer.system.Players[playerID].currentBet = 0;
		dealer.maxBet = 0;
		dealer.system.Players[playerID].UpdateUI();
	}
	
	[RPC]
	public void DistributeFakeCard (int playerID, int position, bool returned)
	{
		//create a new fake card, a bit randomly just to fake to make others players think they can really see the back of the playerID card
		//for non cheat purpose, we never send real cards through network, so no one can know em
		Cards toDeal = new Cards(Instantiate(CardsSplitter.toolsSplitter.Primitive,
		                                     dealer.gDeck.DeckPosition.transform.position,
		                                     Quaternion.Euler(-90,-90,-90)) as GameObject,
		                         CardsSplitter.toolsSplitter.fakeTexture,
		                         14,
		                         Type.Coeur);

		//we distribute a fake center card (i think it should never happen tho, but atleast if someday we need to, its coded)
		//minus one mean its a center card
		if(playerID == -1)
		{
			dealer.centerCards[position].card = toDeal;
			StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toDeal.CardObject,
			                                                       toDeal.CardObject.transform.position,
			                                                       dealer.centerCards[position].CardPosition.position,
			                                                       CardsSplitter.toolsSplitter.partySpeed));
			if(returned)
			{
				StartCoroutine(CoroutinesLauncher.Coroutines.TranslateQuaternion(toDeal.CardObject,
				                                                                 toDeal.CardObject.transform.rotation,
				                                                                 Quaternion.Euler(90,-90,-90),
				                                                                 CardsSplitter.toolsSplitter.partySpeed));
				
			}
		}
		//we dont want players to be able to have the information on a burned card even tho its turned back, again for cheating purpose, we secure it
		//minus two mean its a burned card
		else if(playerID == -2)
		{
			StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toDeal.CardObject,
			                                                       toDeal.CardObject.transform.position,
			                                                       dealer.gDeck.BurnedPosition.position,
			                                                       CardsSplitter.toolsSplitter.partySpeed));
			if(returned)
			{
				StartCoroutine(CoroutinesLauncher.Coroutines.TranslateQuaternion(toDeal.CardObject,
				                                                                 toDeal.CardObject.transform.rotation,
				                                                                 Quaternion.Euler(90,-90,-90),
				                                                                 CardsSplitter.toolsSplitter.partySpeed));
				
			}
		}
		//otherwise its a player card, dont show it and throw some fake again
		else
		{
			dealer.system.Players[playerID].AddCard(toDeal);
			StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toDeal.CardObject, 
			                                                       toDeal.CardObject.transform.position, 
			                                                       dealer.system.Players[playerID].CardsPosition[position].position, 
			                                                       CardsSplitter.toolsSplitter.partySpeed));

			if(returned) dealer.system.Players[playerID].ReturnCards(true);
		}
	}

	[RPC]
	public void DistributeRealCard (int playerID, int cardRank, int cardType, int position, bool returned)
	{
		//find the card you want in the deck, since server deck and client are not sync'd for security, find it in the deck
		//and throw it in the playerID
		Cards toDeal = CardsSplitter.toolsSplitter.FindCard(cardRank, (Type)cardType);

		//minus one mean its a center card
		//center card thrown
		if(playerID == -1)
		{
			dealer.centerCards[position].card = toDeal;
			StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toDeal.CardObject,
			                                                       toDeal.CardObject.transform.position,
			                                                       dealer.centerCards[position].CardPosition.position,
			                                                       CardsSplitter.toolsSplitter.partySpeed));
			if(returned)
			{
				StartCoroutine(CoroutinesLauncher.Coroutines.TranslateQuaternion(toDeal.CardObject,
				                                                                 toDeal.CardObject.transform.rotation,
				                                                                 Quaternion.Euler(90,-90,-90),
				                                                                 CardsSplitter.toolsSplitter.partySpeed));

			}
		}
		//minus two mean its a burned card
		else if(playerID == -2)
		{
			StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toDeal.CardObject,
			                                                       toDeal.CardObject.transform.position,
			                                                       dealer.gDeck.BurnedPosition.position,
			                                                       CardsSplitter.toolsSplitter.partySpeed));

			if(returned)
			{
				StartCoroutine(CoroutinesLauncher.Coroutines.TranslateQuaternion(toDeal.CardObject,
				                                                                 toDeal.CardObject.transform.rotation,
				                                                                 Quaternion.Euler(90,-90,-90),
				                                                                 CardsSplitter.toolsSplitter.partySpeed));
				
			}
		}
		//real player card this function at this state should be called ONLY when you throw the card to the player that own them, or when you want to show every players card
		//it replaces every fake card by real one, then return them, otherwise, never call it, call the FAKE GIVING CARD function
		else
		{
			//this could be translated by if the player got distributed an hidden card already, replace it otherwise just throw the good card to him
			if(position < dealer.system.Players[playerID].cardsInHands.Count)
			{
				Destroy(dealer.system.Players[playerID].cardsInHands[position].CardObject);
				Debug.Log("replaced fake card " + dealer.system.Players[playerID].cardsInHands[position].ToString() + " by " + toDeal.ToString());
				dealer.system.Players[playerID].cardsInHands[position] = toDeal;
				dealer.system.Players[playerID].cardsInHands[position].CardObject.transform.position = dealer.system.Players[playerID].CardsPosition[position].position;

			}
			else
			{
				dealer.system.Players[playerID].AddCard(toDeal);
				
				StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toDeal.CardObject, 
				                                                       toDeal.CardObject.transform.position, 
				                                                       dealer.system.Players[playerID].CardsPosition[position].position, 
				                                                       CardsSplitter.toolsSplitter.partySpeed));
			}
			
			if(returned) dealer.system.Players[playerID].ReturnCards(true);
		}
	}

	[RPC]
	public void ReturnCards (int playerID, bool toShow)
	{
		dealer.system.Players[playerID].ReturnCards(toShow);
	}

	[RPC]
	public void SetDealerAndBlinds (int deal, int littleBlind, int bigBlind)
	{
		dealer.DealerAndBlindsPlayers[0] = dealer.system.Players[deal];
		dealer.DealerAndBlindsPlayers[1] = dealer.system.Players[littleBlind];
		dealer.DealerAndBlindsPlayers[2] = dealer.system.Players[bigBlind];

		dealer.Restart();
	}

	[RPC]
	public void SetPlayerTurn (int playerID)
	{
		foreach(Player p in dealer.system.Players) { p.isCurrentTurn = false; p.UpdateUI(); }

		if(playerID != -1)
		{
			dealer.system.Players[playerID].isCurrentTurn = true;
			dealer.system.Players[playerID].UpdateUI();
		}
	}
}
