using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HandsDefinitions : MonoBehaviour 
{
	public static HandsDefinitions Definitions;
	
	void Start ()
	{ 
		Definitions = this; 
	}


	public List<Hand> CalculateHand (List<Cards> cards)
	{
		if(cards.Count != 7) return null;
		List<Hand> hands = new List<Hand>();

		//single moves
		hands.Add(BestHigh(new List<Cards>(cards)));
		hands.Add(BestPair(new List<Cards>(cards)));
		hands.Add(BestDoublePair(new List<Cards>(cards)));
		hands.Add(BestSet(new List<Cards>(cards)));
		hands.Add(BestFull(new List<Cards>(cards)));
		hands.Add(BestFour(new List<Cards>(cards)));

		//composed moves
		Hand str = BestStraight(new List<Cards>(cards));
		Hand flu = BestFlush(new List<Cards>(cards));

		hands.Add(str);
		hands.Add(flu);

		if(str != null && flu != null)
		{
			//atleast its a straight flush
			hands.Add(new Hand(HandType.StraightFlush, str.GetSecondaryRanks(0)));
			//but if the second rank is also an ace, ITS A ROYAL FLUSH
			if(str.GetSecondaryRanks(0) == 14)
				hands.Add(new Hand(HandType.RoyalFlush, str.GetSecondaryRanks(0))); 
		}

		return hands;
	}

	public Player GetBestEqualityHand (Player first, Player second)
	{
		//find out which hand is the best, if they have the same hand
		int iR = 0;
		while(iR < 5)
		{
			if(first.bestHand.GetSecondaryRanks(iR) > second.bestHand.GetSecondaryRanks(iR)) return first;
			else if(first.bestHand.GetSecondaryRanks(iR) < second.bestHand.GetSecondaryRanks(iR)) return second;
			iR++;
		}

		return null;
	}

	Hand BestHigh (List<Cards> cards)
	{
		List<Cards> FiveOutOfSeven = new List<Cards>();

		cards.Sort();

		for(int i = 0; i < 5; i++) FiveOutOfSeven.Add(cards[i]);

		return new Hand(HandType.High, FiveOutOfSeven[0].Rank, FiveOutOfSeven[1].Rank, FiveOutOfSeven[2].Rank,FiveOutOfSeven[3].Rank, FiveOutOfSeven[4].Rank);
	}

	Hand BestPair (List<Cards> cards)
	{
		List<Cards> FiveOutOfSeven = new List<Cards>();
		List<Cards> Pairs = new List<Cards>();
		cards.Sort();


		//we set the count to 1 since we're searching for pairs
		int count = 1;
		int tempCount, temp = 0;
		
		for (int i = 0; i < cards.Count; i++)
		{
			temp = cards[i].Rank;
			tempCount = 0;
			
			for (int j = 0; j < cards.Count; j++) if (temp == cards[j].Rank) tempCount++;

			if (tempCount > count) Pairs.Add(cards[i]);
		}

		if(Pairs.Count > 1)
		{
			Pairs.Sort();

			//here we set our five best cards
			for(int z = 0; z < 2; z++){ FiveOutOfSeven.Add(Pairs[z]); cards.Remove(Pairs[z]); }

			cards.Sort();

			for(int w = 0; w < 3; w++) FiveOutOfSeven.Add(cards[w]);

			return new Hand(HandType.Pair, FiveOutOfSeven[0].Rank, FiveOutOfSeven[2].Rank, FiveOutOfSeven[3].Rank, FiveOutOfSeven[4].Rank);
		}

		return null;
	}

	Hand BestDoublePair (List<Cards> cards)
	{
		List<Cards> FiveOutOfSeven = new List<Cards>();
		List<Cards> Pairs = new List<Cards>();

		cards.Sort();

		int count = 1;
		int tempCount, temp = 0;
		
		for (int i = 0; i < cards.Count; i++)
		{
			temp = cards[i].Rank;
			tempCount = 0;
			
			for (int j = 0; j < cards.Count; j++) if (temp == cards[j].Rank) tempCount++;
			
			if (tempCount > count) Pairs.Add(cards[i]);
		}
		
		if(Pairs.Count > 3)
		{
			Pairs.Sort();
			
			//here we set our five best cards
			for(int z = 0; z < 4; z++) { FiveOutOfSeven.Add(Pairs[z]); cards.Remove(Pairs[z]); }

			cards.Sort();

			FiveOutOfSeven.Add(cards[0]);
			
			return new Hand(HandType.TwoPair, FiveOutOfSeven[0].Rank, FiveOutOfSeven[2].Rank, FiveOutOfSeven[4].Rank);
		}
		
		return null;
	}

	Hand BestSet (List<Cards> cards)
	{
		List<Cards> FiveOutOfSeven = new List<Cards>();
		List<Cards> Pairs = new List<Cards>();
		
		cards.Sort();
		
		int count = 2;
		int tempCount, temp = 0;
		
		for (int i = 0; i < cards.Count; i++)
		{
			temp = cards[i].Rank;
			tempCount = 0;
			
			for (int j = 0; j < cards.Count; j++) if (temp == cards[j].Rank) tempCount++;
			
			if (tempCount > count) Pairs.Add(cards[i]);
		}
		
		if(Pairs.Count > 2)
		{
			Pairs.Sort();
			
			//here we set our five best cards
			for(int z = 0; z < 3; z++) { FiveOutOfSeven.Add(Pairs[z]); cards.Remove(Pairs[z]); }
			
			cards.Sort();
			
			FiveOutOfSeven.Add(cards[0]);
			FiveOutOfSeven.Add(cards[1]);
			
			return new Hand(HandType.Set, FiveOutOfSeven[0].Rank, FiveOutOfSeven[3].Rank, FiveOutOfSeven[4].Rank);
		}
		
		return null;
	}

	Hand BestStraight (List<Cards> cards)
	{
		cards.Sort();

		int mPossible = cards.Count - 5;
		for(int i = 0; i < mPossible; i++)
		{
			bool broken = false;
			for(int j = 0; j < 5; j++)
			{
				if((cards[i].Rank - j) == cards[i + j].Rank) continue;
				else broken = true;
			}

			if(!broken) return new Hand (HandType.Straight, cards[i].Rank);
		}

		return null;
	}


	Hand BestFlush (List<Cards> cards)
	{
		List<Cards> Flush = new List<Cards>();

		cards.Sort();	
		int count = 4, tempCount = 0;
		Type temp = Type.Carreau;
		
		for (int i = 0; i < cards.Count; i++)
		{
			temp = cards[i].type;
			tempCount = 0;
			
			for (int j = 0; j < cards.Count; j++) if (temp == cards[j].type) tempCount++;
			
			if (tempCount > count) Flush.Add(cards[i]);
		}
		
		if(Flush.Count > 4)
		{
			Flush.Sort();
			return new Hand(HandType.Flush, Flush[0].Rank);
		}

		return null;
	}

	Hand BestFull (List<Cards> cards)
	{
		List<Cards> Sets = new List<Cards>();
		List<Cards> Pairs = new List<Cards>();

		List<Cards> Full = new List<Cards>();
		
		cards.Sort();	

		int tempSet, tempCountSet = 0;
		for (int i = 0; i < cards.Count; i++)
		{
			tempSet = cards[i].Rank;
			tempCountSet = 0;
			
			for (int j = 0; j < cards.Count; j++) if (tempSet == cards[j].Rank) tempCountSet++;
			
			if (tempCountSet > 2 && Sets.Count < 3) Sets.Add(cards[i]);
		}

		foreach(Cards s in Sets) cards.Remove(s);

		int tempPair, tempCountPair = 0;
		for (int x = 0; x < cards.Count; x++)
		{
			tempPair = cards[x].Rank;
			tempCountPair = 0;
			
			for (int y = 0; y < cards.Count; y++) if (tempPair == cards[y].Rank) tempCountPair++;
			
			if (tempCountPair > 1 && Pairs.Count < 2) Pairs.Add(cards[x]);
		}
		
		if(Sets.Count > 2) foreach(Cards S in Sets) Full.Add(S);
		if(Pairs.Count > 1) foreach(Cards P in Pairs) Full.Add(P);

		//Debug.Log(Sets.Count + " : " + Pairs.Count);
		if(Full.Count > 4) return new Hand(HandType.Full, Sets[0].Rank, Pairs[0].Rank);
		
		
		return null;
	}

	Hand BestFour (List<Cards> cards)
	{
		List<Cards> Four = new List<Cards>();
		
		cards.Sort();	
		
		int temp, tempCount = 0;
		for (int i = 0; i < cards.Count; i++)
		{
			temp = cards[i].Rank;
			tempCount = 0;
			
			for (int j = 0; j < cards.Count; j++) if (temp == cards[j].Rank) tempCount++;
			
			if (tempCount > 3) Four.Add(cards[i]);
		}

		if(Four.Count > 3)
		{

			for(int z = 0; z < 4; z++){ cards.Remove(Four[z]); }
			cards.Sort();
			return new Hand(HandType.Four, Four[0].Rank, cards[0].Rank);
		}
		
		
		return null;
	}
}


[System.Serializable]
public class Hand
{
	public HandType type = HandType.High;
	public List<int> ranksInOrder = new List<int>();

	public Hand(HandType _type, int _rank, int OptionnalRankOne = -1, int OptionnalRankTwo = -1, int OptionnalRankThree = -1, int OptionnalRankFour = -1)
	{
		type = _type;
		ranksInOrder.Add(_rank);
		ranksInOrder.Add(OptionnalRankOne);
		ranksInOrder.Add(OptionnalRankTwo);
		ranksInOrder.Add(OptionnalRankThree);
		ranksInOrder.Add(OptionnalRankFour);
	}

	public string typeToString ()
	{
		return type.ToString();
	}

	public int GetRank ()
	{
		return (int)type;
	}

	public int GetSecondaryRanks (int x)
	{
		return ranksInOrder[x];
	}
}

[System.Serializable]
public enum HandType { High = 1, Pair = 2, TwoPair = 3, Set = 4, Straight = 5, Flush = 6, Full = 7, Four = 8, StraightFlush = 9, RoyalFlush = 10 }
