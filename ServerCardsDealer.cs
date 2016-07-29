using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ServerCardsDealer : MonoBehaviour 
{
	public ServerSystem system;
	public Deck gDeck;
	
	public List<CenterCards> centerCards = new List<CenterCards>();
	public List<GameObject> DealerAndBlinds = new List<GameObject>();
	public List<Player> DealerAndBlindsPlayers = new List<Player>();
	
	public Player currentPlayerTurn;
	public bool AllPlayerFeld;

	public int maxBet;

	public int currentPot;

	public int bigBlind;
	public int littleBlind;

	void Start ()
	{
		system = this.GetComponent<ServerSystem>();
		gDeck = new Deck(CardsSplitter.toolsSplitter);
		gDeck.Mix();
		SetDeck();

		for(int i = 0; i < 3; i ++) DealerAndBlindsPlayers.Add(null);
	}

	public IEnumerator InitiateGame ()
	{
		system.IsLaunched = true;
		system.WriteToClientScreen("Starting", new Vector3(-1, -1, -1), 2.0f);
		//here we check who is the dealer

		yield return new WaitForSeconds(3.0f);

		yield return StartCoroutine(FindDealer());
			
		yield return new WaitForSeconds(3.0f);

		Restart();
	}

	/* function to find the dealer */
	IEnumerator FindDealer ()
	{
		for(int i = 0; i < system.Players.Count; i++)
		{
			while (true)
			{
				Cards toDeal = gDeck.GetCard();

				if(toDeal.type != Type.Trèfle) continue;
				else
				{
					system.Players[i].AddCard(toDeal);
					StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toDeal.CardObject, 
					                                                       toDeal.CardObject.transform.position, 
					                                                       system.Players[i].CardsPosition[0].position, 
					                                                       CardsSplitter.toolsSplitter.partySpeed));
					/* here start the network part */
					system.Distribute(DistributeType.Shown, DistributeTarget.All, i, false, toDeal);
					/* here end the network part */
				
					yield return new WaitForSeconds(0.2f);
					break ;
				}
			}
		}

		yield return new WaitForSeconds(2.0f);

		int bestRank = 0;
		for(int j = 0; j < system.Players.Count; j++)
		{
			if(system.Players[j].cardsInHands[0].Rank > bestRank)
			{
				bestRank = system.Players[j].cardsInHands[0].Rank;
				DealerAndBlindsPlayers[0] = system.Players[j];
			}
			system.Players[j].ReturnCards(true);

			/* here start the network part */
			system.ReturnCards(DistributeType.Shown, DistributeTarget.All, j);
			/* here end the network part */
		}

		CalculateBlindsAndDealer(false);

		yield return new WaitForSeconds(1.0f);
	}

	/* THIS FUNCTION IS THE MAIN FUNCTION WHICH LEAD THE GAME SEQUENCE */
	IEnumerator GameSequences ()
	{
		//we give cards to all players
		yield return StartCoroutine(DealCards());

		//start the first betting round
		yield return StartCoroutine(ProcessTurns(2));
		while (!IsBetEquality()) yield return StartCoroutine(ProcessTurns(2));
		SetPot();

		//then we show the flop
		yield return StartCoroutine(DealCenterCards(0, 3));

		//start the second betting round
		yield return StartCoroutine(ProcessTurns(1));
		while (!IsBetEquality()) yield return StartCoroutine(ProcessTurns(1));
		SetPot();

		//then we show the turn
		yield return StartCoroutine(DealCenterCards(3, 4));

		//start the third betting round
		yield return StartCoroutine(ProcessTurns(1));
		while (!IsBetEquality()) yield return StartCoroutine(ProcessTurns(1));
		SetPot();

		//and for last we show the fully river
		yield return StartCoroutine(DealCenterCards(4, 5));

		//start the last betting round
		yield return StartCoroutine(ProcessTurns(1));
		while (!IsBetEquality()) yield return StartCoroutine(ProcessTurns(1));
		SetPot();
	
		//we distribute every real cards by going loop exclude
		for (int i = 0; i < system.Players.Count; i++)
		{
			if (system.Players[i].cardsInHands.Count < 2) continue;
			for (int j = 0; j < 2; j++)
				system.Distribute(DistributeType.Shown, DistributeTarget.Exclude, i, true, system.Players[i].cardsInHands[j], j);
		}

		//then we return cards
		yield return StartCoroutine(ReturnCards());

		//after everyone looked at theirs card we calculate which ones won
		List<Player> winnerRound = ProcessWinners();

		//find out how many chips winner(s) won
		int chipGain = currentPot / winnerRound.Count;

		//show the winners to everyone
		system.SetRoundWinners(winnerRound, chipGain);

		//reset player chips
		for(int j = 0; j < winnerRound.Count; j++)
		{
			if (winnerRound[j] == null) continue;
			winnerRound[j].chips += chipGain;
			system.SetChips(system.Players.IndexOf(winnerRound[j]), chipGain);
		}

		//waiting time for the next round
		yield return new WaitForSeconds(5.0f);

		//calculate the new dealers but move clockwise

		Player validPlayer = IsMatchOver();
		if(validPlayer == null)
		{
			CalculateBlindsAndDealer(true);
			system.RemoveDisconnectedPlayers();
			Restart();
		}
		else
		{
			system.WriteToClientScreen("Game is over \n" + validPlayer.pseudo + " won the game !", new Vector3(-1, -1, -1), -1.0f);
			yield return new WaitForSeconds(10.0f);
			system.WriteToClientScreen("You will be disconnected in 10 seconds, if you want to play again, reconnect", new Vector3(-1, -1, -1), -1.0f);
			yield return new WaitForSeconds(10.0f);
			system.CleanupServer();
		}
	}
	
	IEnumerator DealCards ()
	{
		List<Player> modifiedList = new List<Player>();
		List<int> ApplicablePlaces = new List<int>();

		// get the list of places players are using
		foreach (Player p in system.Players)
			ApplicablePlaces.Add(p.place);

		// sort it so its easy to find out which one is the next
		ApplicablePlaces.Sort();

		// get the index of the first player to play
		int playerIndex = ApplicablePlaces.IndexOf(DealerAndBlindsPlayers[0].place);

		// get every valid player until the end of the list
		for (int x = playerIndex; x < ApplicablePlaces.Count; x++)
		{
			// get the player were testing right now (by its place)
			Player p = GetPlayerByPlace(ApplicablePlaces[x]);
			// valid player
			if (p.chips <= 0)
				p.isFeld = true;
			else
				modifiedList.Add(p);
		}
		//now get everything from the start to the first player to play
		for (int y = 0; y < playerIndex; y++)
		{
			// get the player were testing right now (by its place)
			Player p = GetPlayerByPlace(ApplicablePlaces[y]);
			// valid player
			if (p.chips <= 0)
				p.isFeld = true;
			else
				modifiedList.Add(p);
		}

		//here we distribute the card, hidden or shown, it all depends of the player authority
		for(int i = 0; i < 2; i++)
		{
			for(int z = 0; z < modifiedList.Count; z++)
			{
				Cards toDeal = gDeck.GetCard();
				
				modifiedList[z].AddCard(toDeal);
				StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toDeal.CardObject, 
				                                                       toDeal.CardObject.transform.position, 
				                                                       modifiedList[z].CardsPosition[i].position, 
				                                                       CardsSplitter.toolsSplitter.partySpeed));

				//----------------------------
				//-here start the network part
				//----------------------------
				system.Distribute(DistributeType.Hidden, DistributeTarget.Exclude, system.Players.IndexOf(modifiedList[z]), false, position:i );
				system.Distribute(DistributeType.Shown, DistributeTarget.Include, system.Players.IndexOf(modifiedList[z]), true, toDeal, i);

				modifiedList[z].ReturnCards(true);
				//--------------------------
				//-here end the network part
				//--------------------------
				yield return new WaitForSeconds(0.2f);
			}
		}
	}
	
	IEnumerator ProcessTurns (int decalage)
	{
		List<Player> listOfTurns = CalculatePlayersTurn(decalage);

		if(listOfTurns.Count > 1 || (listOfTurns.Count == 1 && listOfTurns[0].currentBet != maxBet))
		{
			int feldPlayersDuringTurn = 0;
			foreach (Player p in listOfTurns)
			{
				//here we set the flag that this is this player turn and he should play
				p.isCurrentTurn = true;
				currentPlayerTurn = p;
				p.UpdateUI();

				/* network part */
				//set to every player which one should play
				system.SetPlayerTurn(system.Players.IndexOf(p));
				/* network part */

				//then we start the timer to see if the player is afk or not and we wait for an action from him
				float t = 0.0f;
				while (t <= CardsSplitter.toolsSplitter.timePerTurn)
				{
					//if our flag got triggered by an action of the player just break the loop and go to the next player
					if (!p.isCurrentTurn) break;
					// if hes disconnected, just end his reflexion timing and put him on fold artificially, we'll remove him from the pool at the end.
					if (p.isDisconnected) t += (CardsSplitter.toolsSplitter.timePerTurn * 2);

					t += Time.deltaTime;

					//to have a smooth ending
					if (t >= CardsSplitter.toolsSplitter.timePerTurn - 0.8f) t = CardsSplitter.toolsSplitter.timePerTurn + 0.5f;
					yield return null;
				}

				//if the player made no answer and used all the time, he is afk so fold out
				if (t >= CardsSplitter.toolsSplitter.timePerTurn)
				{ 
					system.SetFold(system.Players.IndexOf(p)); 
					p.Fold(); 
					p.isCurrentTurn = false; 
				}
				p.UpdateUI();


				if(p.isFeld) feldPlayersDuringTurn++;

				//if we have only one player left or even less break the loop and leave
				if(feldPlayersDuringTurn >= listOfTurns.Count - 1) break;
			}
		}
		else AllPlayerFeld = true;

		//no one left set turn to no one
		system.SetPlayerTurn(-1);
	}
	
	IEnumerator DealCenterCards (int st, int end)
	{

		Cards toBurn = gDeck.GetCard();
		system.Distribute(DistributeType.Hidden, DistributeTarget.All, -2, false);
		yield return StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toBurn.CardObject, toBurn.CardObject.transform.position, gDeck.BurnedPosition.position, CardsSplitter.toolsSplitter.partySpeed * 5.0F));
		
		yield return new WaitForSeconds(0.2f);

		for(int i = st; i < end; i++)
		{
			Cards toDeal = gDeck.GetCard();
			centerCards[i].card = toDeal;
			StartCoroutine(CoroutinesLauncher.Coroutines.Translate(toDeal.CardObject, toDeal.CardObject.transform.position, centerCards[i].CardPosition.position, CardsSplitter.toolsSplitter.partySpeed));
			StartCoroutine(CoroutinesLauncher.Coroutines.TranslateQuaternion(toDeal.CardObject, toDeal.CardObject.transform.rotation, Quaternion.Euler(90,-90,-90), CardsSplitter.toolsSplitter.partySpeed));

			system.Distribute(DistributeType.Shown, DistributeTarget.All, -1, true, toDeal, position: i);

			yield return new WaitForSeconds(0.2f);
		}
	}

	public IEnumerator ReturnCards ()
	{
		foreach(Player p in system.Players) { p.ReturnCards(true); yield return new WaitForSeconds(0.15f); }
	}

	public void SetDeck ()
	{
		float deckSpace = 0.0f;
		foreach(Cards c in gDeck.cards)
		{
			c.CardObject.transform.position = new Vector3(gDeck.DeckPosition.transform.position.x, gDeck.DeckPosition.transform.position.y + deckSpace, gDeck.DeckPosition.transform.position.z);
			c.CardObject.transform.rotation = Quaternion.Euler(-90,-90,-90);
			deckSpace += 0.0065f;
		}
	}

	public bool IsBetEquality ()
	{
		//check if the max bet is the same bet to everyone
		foreach(Player p in system.Players)
		{
			if(!p.isFeld)
			{
				Debug.Log(maxBet.ToString() + " : " + p.currentBet + " : " + p.chips.ToString());
				if(p.currentBet != maxBet && p.chips > 0) return false;
			}
		}

		//if all is ok just return true;
		return true;
	}

	//if there is only one left player, return the last player alive, otherwise return null
	public Player IsMatchOver ()
	{
		int nb = 0;
		Player validPlayer = null;

		foreach (Player p in system.Players)
		{
			if (p.isDisconnected) continue;
			if (p.chips > 0)
			{
				nb++;
				validPlayer = p;
			}
		}

		if (nb == 1) return validPlayer;
		else if (nb == 0) return new Player("All Disconnected");
		else return null;
	}

	public List<Player> ProcessWinners ()
	{
		foreach(Player p in system.Players)
		{
			if(p.isFeld) continue;
			//set a new list with the seven cards
			List<Cards> TotalCards = CardsSplitter.toolsSplitter.MergeHands(p.cardsInHands, centerCards);
			//here we calculate the hand of the player with the sevens cards
			p.SetBestHand(HandsDefinitions.Definitions.CalculateHand(TotalCards));
		}

		//once we calculated all players best hand we gotta calculate which one is the best from all players

		int bestHand = 0;
		List<Player> bestPlayer = new List<Player>();
		bestPlayer.Add(null);
		foreach(Player player in system.Players)
		{
			if(player.isFeld || player.bestHand == null) continue;
			int handPlayer = player.bestHand.GetRank();
			
			if(handPlayer > bestHand) { bestHand = handPlayer; bestPlayer[0] = player; }
			//if there is equality we check inner rank (like high values etc)
			else if(handPlayer == bestHand)
			{
				Player equalitycheck = HandsDefinitions.Definitions.GetBestEqualityHand(bestPlayer[0], player);

				//it mean there is a pure equality on all cards, which mean we will give money to both player so we add him in the list of the winners
				if(equalitycheck == null) bestPlayer.Add(player);
				//otherwise the best player is the one which won it
				else bestPlayer[0] = equalitycheck;
			}
		}

		return bestPlayer;
	}

	public void Cleanup()
	{
		foreach (Player p in system.Players)
		{
			p.currentBet = 0;
			p.isFeld = false;
			p.RemoveCards();
		}

		foreach (CenterCards c in centerCards) c.card = null;

		//here we set the deck by reseting it, mixing it, and form the objets
		gDeck.Reset(CardsSplitter.toolsSplitter);
		gDeck.Mix();
		SetDeck();
		currentPot = 0;
	}

	public void Restart () 
	{
		Cleanup();

		//goes to the server part, find out dealer
		//set blinds
		//and start the game sequence
		if(Network.isServer)
		{
			AllPlayerFeld = false;

			system.SetDealerAndBlinds(system.Players.IndexOf(DealerAndBlindsPlayers[0]), 
			                          system.Players.IndexOf(DealerAndBlindsPlayers[1]), 
			                          system.Players.IndexOf(DealerAndBlindsPlayers[2]));

			DealerAndBlindsPlayers[1].chips -= littleBlind;
			DealerAndBlindsPlayers[1].currentBet += littleBlind;
			
			DealerAndBlindsPlayers[2].chips -= bigBlind;
			DealerAndBlindsPlayers[2].currentBet += bigBlind;;

			system.SetBet(system.Players.IndexOf(DealerAndBlindsPlayers[1]), littleBlind);
			system.SetBet(system.Players.IndexOf(DealerAndBlindsPlayers[2]), bigBlind);

			StartCoroutine(GameSequences());
		}

		SetBlindsAndDealers ();
	}

	Player GetPlayerByPlace(int place)
	{
		foreach (Player p in system.Players)
			if (p.place == place)
				return (p);
		return (null);
	}

	// receive a applicable list sorted
	int GetNextValidPlace(int place, List<int> ApplicableList)
	{
		// im just searching for the next place that is greater than place
		for (int i = 0; i < ApplicableList.Count; i++)
			if (ApplicableList[i] > place)
				return ApplicableList[i];
		// if i didnt find anyone, it mean our place is the last one, so take to the first valid place
		return ApplicableList[0];
	}

	public void CalculateBlindsAndDealer (bool moving)
	{
		List<int> ApplicablePlaces = new List<int>();
		// get a list of valid places
		foreach (Player p in system.Players)
			if (p.chips > 0 && !p.isDisconnected)
				ApplicablePlaces.Add(p.place);

		// sort it so its easy to find out which one is the next
		ApplicablePlaces.Sort();

		// if we have to go forward
		// pretty straight forward, set dealer to the next valid place
		if (moving) DealerAndBlindsPlayers[0] = GetPlayerByPlace(GetNextValidPlace(DealerAndBlindsPlayers[0].place, ApplicablePlaces));
		// now, the dealer is forced to be at the place we wanted it to be, setup small blind to be the next place of the dealer
		DealerAndBlindsPlayers[1] = GetPlayerByPlace(GetNextValidPlace(DealerAndBlindsPlayers[0].place, ApplicablePlaces));
		// and the big blind to be the next place to the small blind
		DealerAndBlindsPlayers[2] = GetPlayerByPlace(GetNextValidPlace(DealerAndBlindsPlayers[1].place, ApplicablePlaces));

	}

	public void SetBlindsAndDealers ()
	{
		for(int i = 0; i < 3; i++) StartCoroutine(CoroutinesLauncher.Coroutines.Translate(DealerAndBlinds[i], 
		                                                                                  DealerAndBlinds[i].transform.position,
		                                                                                  DealerAndBlindsPlayers[i].DealerPosition.position,
		                                                                                  CardsSplitter.toolsSplitter.partySpeed));
	}

	public void SetPot ()
	{
		for(int i = 0; i < system.Players.Count; i++)
		{
			system.AddPot(i);

			currentPot += system.Players[i].currentBet;
			system.Players[i].currentBet = 0;
			system.Players[i].UpdateUI();
		}

		maxBet = 0;
	}

	public List<Player> CalculatePlayersTurn (int offset)
	{
		List<Player> listOfTurns = new List<Player>();

		List<int> ApplicablePlaces = new List<int>();
		// get the list of places players are using
		foreach (Player p in system.Players)
			ApplicablePlaces.Add(p.place);

		// sort it so its easy to find out which one is the next
		ApplicablePlaces.Sort();

		// get the index of the first player to play
		int playerIndex = ApplicablePlaces.IndexOf(DealerAndBlindsPlayers[offset].place);

		// get every valid player until the end of the list
		for (int x = playerIndex; x < ApplicablePlaces.Count; x++)
		{
			// get the player were testing right now (by its place)
			Player p = GetPlayerByPlace(ApplicablePlaces[x]);
			// valid player
			if (!p.isFeld && p.chips > 0)
				listOfTurns.Add(p);
		}
		//now get everything from the start to the first player to play
		for (int y = 0; y < playerIndex; y++)
		{
			Player p = GetPlayerByPlace(ApplicablePlaces[y]);
			if (!p.isFeld && p.chips > 0)
				listOfTurns.Add(p);
		}
		return (listOfTurns);
	}
}


[System.Serializable]
public class Deck
{
	public Transform DeckPosition;
	public Transform BurnedPosition;
	public List<Cards> cards = new List<Cards>();

	public Deck (CardsSplitter tools)
	{
		for(int i = 0; i < CardsSplitter.toolsSplitter.CardsPacks.Count; i++)
		{
			int CardsCount = 0;
			for(int x = 0; x < tools.CardsPacks[i].SpriteLength.x; x++)
			{
				for(int y = 0; y < tools.CardsPacks[i].SpriteLength.y; y++)
				{
					Texture2D SplittedTex = tools.SplitTexture2D(CardsSplitter.toolsSplitter.CardsPacks[i].TexturePack, 
					                                             (int) (x * tools.CardsPacks[i].CardParser.x), 
					                                             (int) (y * tools.CardsPacks[i].CardParser.y), 
					                                             (int) tools.CardsPacks[i].CardParser.width,
					                                             (int) tools.CardsPacks[i].CardParser.height);
				
					tools.CardsPacks[i].LCards[CardsCount].texture = SplittedTex;
					tools.CardsPacks[i].LCards[CardsCount].CardObject = GameObject.Instantiate(tools.Primitive) as GameObject;
					tools.CardsPacks[i].LCards[CardsCount].CardObject.renderer.material.mainTexture = SplittedTex;
					tools.allCards.Add(tools.CardsPacks[i].LCards[CardsCount]);
					CardsCount++;
				}
			}
		}

		DeckPosition = GameObject.Find("Server").transform.GetChild(0);
		BurnedPosition = GameObject.Find("Server").transform.GetChild(1);

		Reset(tools);
	}
	
	public void Reset (CardsSplitter tools) 
	{ 
		cards.Clear();

		foreach(TexturePackCards TPC in tools.CardsPacks)
		{
			foreach(Cards C in TPC.LCards)
			{
				cards.Add(C);
				C.CardObject.transform.localEulerAngles = new Vector3(-90,-90,-90);
			}
		}
	}

	public void Mix ()
	{
		List<int> RandomizerPattern = new List<int>();
		List<Cards> cardsBackup = new List<Cards>();
		
		foreach(Cards c in cards) 
		{
			cardsBackup.Add(c);
			
			
			//we loop until we found the good random card
			bool RandomCardFound = false;
			while(!RandomCardFound)
			{
				int randomizer = Random.Range(0, cards.Count);
				
				//here we test if we mixed a card that we already picked up
				bool broken = false;
				for(int x = 0; x < RandomizerPattern.Count; x++)
				{
					if(randomizer == RandomizerPattern[x]) { broken = true; break; }
				}
				
				if(!broken)
				{
					RandomizerPattern.Add(randomizer); 
					RandomCardFound = true;
				}
			}
			
			//if we go through this that mean we actually found the good card
		}

		for(int i = 0; i < RandomizerPattern.Count; i++)
		{
			cards[i] = cardsBackup[RandomizerPattern[i]];
		}
	}
	
	public void Spread ()
	{
		float espace = 0.0f;
		foreach(Cards c in cards)
		{
			c.CardObject.transform.position = new Vector3(espace, 0.0f, 0.0f);
			espace += 1.0f;
		}
	}

	public void AddCard (Cards c)
	{
		cards.Add(c);

		CardsSplitter.toolsSplitter.dealer.SetDeck();
	}
	
	public Cards GetCard ()
	{
		if(cards.Count > 0)
		{
			Cards toDeal = cards[cards.Count - 1];
			cards.Remove(toDeal);
			return toDeal;
		}
		return null;
	}
}
