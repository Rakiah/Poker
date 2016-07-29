using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HUD : MonoBehaviour 
{
	public Client self;
	public Button Raise;
	public Button CheckFollow;
	public Text checkFollowText;
	public Button Fold;

	public Text Pot;
	public Text winRound;

	public Text ConnectButton;
	public Text ConnectLabel;

	public GameObject Actions;

	public Text NameText;
	public string IP;
	public int Port;

	void OnConnectedToServer()
	{
		ConnectLabel.text = "Done losing ?";
		ConnectButton.text = "Disconnect";
	}

	void OnDisconnectedFromServer()
	{
		Application.LoadLevel(Application.loadedLevel);
	}
	
	void Update ()
	{
		if(self.selfPlayer.isCurrentTurn)
		{
			Raise.interactable = true;
			CheckFollow.interactable = true;
			Fold.interactable = true;

			if (self.selfPlayer.currentBet < self.dealer.maxBet)
			{
				if (self.selfPlayer.currentBet + self.selfPlayer.chips <= self.dealer.maxBet)
					checkFollowText.text = "All-In";
				else 
					checkFollowText.text = "Follow";
			}
			else checkFollowText.text = "Check";
		}
		else
		{
			Raise.interactable = false;
			CheckFollow.interactable = false;
			Fold.interactable = false;
		}

		Pot.text = self.dealer.currentPot.ToString();
	}

	public IEnumerator SetTextUI(string str, Vector3 position, float timer)
	{
		winRound.enabled = true;
		Actions.SetActive(false);
		winRound.text = str;
		if(position.x != -1 && position.y != -1) winRound.rectTransform.position = position;
		yield return new WaitForSeconds(timer);
		winRound.enabled = false;
		Actions.SetActive(true);
	}

	public void SetTextUI(string str, Vector3 position)
	{
		winRound.enabled = true;
		Actions.SetActive(false);
		winRound.text = str;
		if(position.x != -1 && position.y != -1) winRound.rectTransform.position = position;
	}

	public void Connect_Or_Disconnect ()
	{
		if (Network.isClient)
			Network.Disconnect();
		else
			Network.Connect(IP, Port);
	}
}
