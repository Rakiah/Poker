using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ServerSystem : MonoBehaviour 
{
	ServerCardsDealer	dealer;
	
	public int			MinimumPlayers;
	public int			MaximumPlayers;

	public List<Player> Players = new List<Player>();
	public List<int>	PlacesLefts = new List<int>();

	private IEnumerator	CountStartRoutine;

	public bool			IsLaunched;

	public bool			IsServer;

	public float			WaitForStart = 5.0f;

	public int			Port = 5679;


	void Start ()
	{
		dealer = GetComponent<ServerCardsDealer>();

		if (IsServer) { Network.InitializeServer(10, Port, false); }
	}
	
	void OnPlayerConnected (NetworkPlayer playerconnected)
	{
		if (Players.Count + 1 > MaximumPlayers)
		{
			// should handle multiple game instance later but thats for another day.
			WriteToClientScreen("Sorry, game is full, try again later", new Vector3(-1, -1, -1), 5.0f, playerconnected);
			Network.CloseConnection(playerconnected, true);
			return;
		}
		if (IsLaunched)
		{
			// should handle spectating game later but thats for another day.
			WriteToClientScreen("Sorry, game has already started, try again later", new Vector3(-1, -1, -1), 5.0f, playerconnected);
			Network.CloseConnection(playerconnected, true);
			return;
		}

		Player p = new Player(PlacesLefts[0], playerconnected, 500);
		Players.Add(p);
		p.UpdateUI();
		PlacesLefts.RemoveAt(0);
		UpdateInformations();
	}
	
	void OnPlayerDisconnected (NetworkPlayer playerdisconnected)
	{
		Player p = getPlayer(playerdisconnected);
		if (p == null) return;
		if (IsLaunched) p.isDisconnected = true;
		else
		{
			RemoveDisconnectedPlayer(Players.IndexOf(p));
			if (CountStartRoutine != null)
				StopCoroutine(CountStartRoutine);
			if (Players.Count >= MinimumPlayers)
			{
				CountStartRoutine = CountUntilStart();
				StartCoroutine(CountStartRoutine);
			}
			else
				WriteToClientScreen("Too much players left, start cancelled", new Vector3(-1, -1, -1), 5.0f);
		}
	}

	public void RemoveDisconnectedPlayers()
	{
		for (int i = 0; i < Players.Count; i++)
			if (Players[i].isDisconnected)
				RemoveDisconnectedPlayer(i);
	}

	void RemoveDisconnectedPlayer(int id)
	{
		Player p = Players[id];
		PlacesLefts.Add(p.place);
		p.Release();
		Players.Remove(p);
		CoroutinesLauncher.Coroutines.networkView.RPC("DeletePlayer", RPCMode.Others, id);
	}


	public Player getPlayer (NetworkPlayer _player)
	{
		foreach(Player p in Players) if(_player == p.player) return p;
		return null;
	}
	
	public int getPlayerID(NetworkPlayer _player)
	{
		for(int i = 0; i < Players.Count; i++) if(_player == Players[i].player) return i;
		return -1;
	}

	public void CleanupServer()
	{
		Network.Disconnect();
		Application.LoadLevel(Application.loadedLevel);
	}

	public void Distribute (DistributeType type, 
	                        DistributeTarget target, 
	                        int optPlayer, 
	                        bool returned, 
	                        Cards toDeal = null, 
	                        int position = 0)
	{
		List<Player> modifiedList = new List<Player>(Players);

		if(target == DistributeTarget.Exclude)
			modifiedList.RemoveAt(optPlayer);
		else if(target == DistributeTarget.Include)
		{
			modifiedList.Clear();
			modifiedList.Add(Players[optPlayer]);
		}

		for(int i = 0; i < modifiedList.Count; i++)
		{
			if (modifiedList[i].isDisconnected) continue;
			if(type == DistributeType.Hidden) 
				CoroutinesLauncher.Coroutines.networkView.RPC("DistributeFakeCard",
				                                              modifiedList[i].player,
				                                              optPlayer,
				                                              position,
				                                              returned);
			else 
				CoroutinesLauncher.Coroutines.networkView.RPC("DistributeRealCard",
				                                              modifiedList[i].player,
				                                              optPlayer,
				                                              toDeal.Rank,
				                                              (int)toDeal.type, position,
				                                              returned);
		}
	}

	public void ReturnCards (DistributeType type, DistributeTarget target, int optPlayer)
	{
		List<Player> modifiedList = new List<Player>(Players);
		
		if(target == DistributeTarget.Exclude) modifiedList.RemoveAt(optPlayer);
		else if(target == DistributeTarget.Include) { modifiedList.Clear(); modifiedList.Add(Players[optPlayer]); }
		
		for(int i = 0; i < modifiedList.Count; i++)
		{
			if (modifiedList[i].isDisconnected) continue;
			CoroutinesLauncher.Coroutines.networkView.RPC("ReturnCards",
			                                              modifiedList[i].player,
			                                              optPlayer,
			                                              type == DistributeType.Shown ? true : false);
		}
	}

	public void SetDealerAndBlinds (int deal, int littleBlind, int bigBlind)
	{
		CoroutinesLauncher.Coroutines.networkView.RPC("SetDealerAndBlinds", RPCMode.Others, deal, littleBlind, bigBlind);
	}

	public void SetBet (int playerID, int bet)
	{
		if(Players[playerID].currentBet > dealer.maxBet) dealer.maxBet = Players[playerID].currentBet;
		Players[playerID].UpdateUI();
		CoroutinesLauncher.Coroutines.networkView.RPC("SetBet", RPCMode.Others, playerID, bet);
	}

	public void SetChips (int playerID, int chips)
	{
		Players[playerID].UpdateUI();
		CoroutinesLauncher.Coroutines.networkView.RPC("SetChips", RPCMode.Others, playerID, chips);
	}

	public void SetFold (int playerID)
	{
		Players[playerID].UpdateUI();
		CoroutinesLauncher.Coroutines.networkView.RPC("SetFold", RPCMode.Others, playerID);
	}

	public void SetRoundWinners (List<Player> winners, int chipGain)
	{
		string toWrite = "";

		toWrite = "Player(s) :" + "\n";
		foreach (Player p in winners)
		{
			if (p == null) continue;
			toWrite += p.pseudo + " : " + p.place + "\n";
		}
		
		toWrite += "\n";
		toWrite += "won hand and " + chipGain + " chips !";

		WriteToClientScreen(toWrite, new Vector3(-1, -1, -1), 4.0f);
	}

	public void WriteToClientScreen(string str, Vector3 position, float timer)
	{
		CoroutinesLauncher.Coroutines.networkView.RPC("SetTextUI", RPCMode.Others, str, position, timer);
	}

	public void WriteToClientScreen(string str, Vector3 position, float timer, NetworkPlayer player)
	{
		CoroutinesLauncher.Coroutines.networkView.RPC("SetTextUI", player, str, position, timer);
	}

	public void SetPlayerTurn (int playerID)
	{
		CoroutinesLauncher.Coroutines.networkView.RPC("SetPlayerTurn", RPCMode.Others, playerID);
	}

	public void AddPot (int playerID)
	{
		CoroutinesLauncher.Coroutines.networkView.RPC("AddPot", RPCMode.Others, playerID);
	}


	public void UpdateInformations ()
	{
		for (int i = 0; i < Players.Count; i++)
		{
			CoroutinesLauncher.Coroutines.networkView.RPC("GetInformations", RPCMode.Others, i, Players[i].place, Players[i].chips, Players[i].pseudo, Players[i].player);
		}
	}

	public void UpdateChipCount (int i)
	{
		CoroutinesLauncher.Coroutines.networkView.RPC("GetChipCount", RPCMode.Others, i, Players[i].chips);
	}

	public IEnumerator CountUntilStart()
	{
		float Decount = 0.0f;
		float t = 0.0f;
		while (t < WaitForStart)
		{
			Decount += Time.deltaTime;
			t += Time.deltaTime;

			if (Decount >= 1.0f)
			{
				WriteToClientScreen("Time before start : " + ((int)(WaitForStart - t)).ToString(), new Vector3(-1, -1, -1), -1.0f);
				Decount = 0.0f;
			}
			yield return null;
		}
		StartCoroutine(dealer.InitiateGame());
	}


	[RPC]
	public void SetClientInformations (int id, string pseudo)
	{
		Players[id].pseudo = pseudo;
		Players[id].UpdateUI();
		UpdateInformations();

		if (CountStartRoutine != null)
			StopCoroutine(CountStartRoutine);

		if (Players.Count == MaximumPlayers)
			StartCoroutine(dealer.InitiateGame());
		else if (Players.Count >= MinimumPlayers)
		{
			CountStartRoutine = CountUntilStart();
			StartCoroutine(CountStartRoutine);
		}
	}

	[RPC]
	public void GetRaiseCall (NetworkMessageInfo info)
	{
		Player p = dealer.currentPlayerTurn;
		if(info.sender != p.player) return;

		//raise his bet
		int bet = p.Raise(dealer.maxBet, dealer.bigBlind);

		//then we send to everyone his bet
		SetBet(Players.IndexOf(p), bet);
	}

	[RPC]
	public void GetCheckFollowCall (NetworkMessageInfo info)
	{
		Player p = dealer.currentPlayerTurn;
		if(info.sender != p.player) return;

		int bet = p.CheckFollow(dealer.maxBet);


		SetBet(Players.IndexOf(p), bet);
	}

	[RPC]
	public void GetFoldCall (NetworkMessageInfo info)
	{
		Player p = dealer.currentPlayerTurn;
		if(info.sender != p.player) return;

		p.Fold();
		SetFold(Players.IndexOf(p));
	}
}

public enum DistributeTarget { Exclude, Include, All }
public enum DistributeType { Shown, Hidden }

