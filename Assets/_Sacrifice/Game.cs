using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoBehaviour {

	public enum Mode
	{
		TitleScreen,
		Draft, EndDraft, Prepare,
		BeginTurn, Turn, EndTurn,
		GameOver
	};
	struct Player
	{
		public void Reset () { score = 0; mode = 0; }
		public Deck deck;
		public Deck hand;
		public int score;
		public Mode mode;
		//public int currentMode; public int pendingMode;
	};
	private Player[] players;
	private int currentPlayer = 0;
	public int playerCount = 2;
	private Deck InstantiateDeck (Deck from)
	{
		Deck newDeck = (Instantiate (from.gameObject) as GameObject).GetComponent<Deck>();
		newDeck.order = from.order;
        newDeck.transform.parent = from.transform.parent;
		newDeck.transform.localPosition = from.transform.localPosition;
		newDeck.maxCardsSpace = from.maxCardsSpace;
		return newDeck;
	}
	private void InitPlayer ()
	{
		if (players[currentPlayer].deck == null)
			players[currentPlayer].deck = InstantiateDeck (_deck);
		if (players[currentPlayer].hand == null)
			players[currentPlayer].hand = InstantiateDeck (_hand);
		players[currentPlayer].deck.Visible = true;
		players[currentPlayer].hand.Visible = true;
	}
	private void NextPlayer ()
	{
		players[currentPlayer].deck.Visible = false;
		players[currentPlayer].hand.Visible = false;

		//players[currentPlayer].currentMode = players[currentPlayer].pendingMode;;
				
		currentPlayer = (currentPlayer + 1) % playerCount;

		if (players[currentPlayer].deck)
			players[currentPlayer].deck.Visible = true;
		if (players[currentPlayer].hand)
			players[currentPlayer].hand.Visible = true;
	}
	private int TopPlayer {
		get {
			int topScore = 0;
			int topPlayer = 0;
			for (int q = 0; q < playerCount; ++q)
				if (players[q].score > topScore)
				{
					topScore = players[q].score;
					topPlayer = q;
				}
			return topPlayer;
		}
	}


	public static Game Instance;

	public GameObject ingridientDeckPrefab;
	public GameObject cardPrefab;
    public CardDesc[] cards;
    public CardDesc[] ingredientCards;

	public Deck draft;
	//public Deck deck;
	//public Deck hand;
	public Deck _deck;
	public Deck _hand;
	public Deck deck { get { return players[currentPlayer].deck; }}
	public Deck hand { get { return players[currentPlayer].hand; }}
	public int score { get { return players[currentPlayer].score; } set { players[currentPlayer].score = value; } }
	//public int mode { get { return players[currentPlayer].currentMode; } set { players[currentPlayer].pendingMode = value; } }
	public Mode mode { get { return players[currentPlayer].mode; } set { players[currentPlayer].mode = value; } }
	public Deck ingridients;
	public Deck pool;
	public Deck discard;
	public Text infoText;
	public Text scoreText;
	public Button endTurnButton;
	public Renderer background;
	public Texture bgTexture;

	public int maxScore = 30;
	public int boosterSize = 10;
	public int fullDeckSize = 20;
	public int openingHandSize = 5;
	public int maxHandSize = 10;
	private int poolSize = 4;

	public bool autoDraft = false;
	public string forcedAction = "";

	void Start ()
	{
		players = new Player[playerCount];
		/*Sprite[] allSprites = Resources.LoadAll<Sprite>("");
		foreach (var s in allSprites)
			Debug.Log (s.name);
		*/
		//for (int q = 0; q > 8; ++q)
		//	cards[q].action = forceAction;

		poolSize = playerCount;

		Instance = this;
		discard.CreateCards (cards, playerCount);
		discard.Shuffle();
		discard.Visible = false;
		ingridients.CreateCards (ingredientCards, 2*playerCount);
		ingridients.Shuffle();
		ingridients.Visible = false;

		_deck.maxCardsSpace = fullDeckSize;

		// "prewarm" players
		foreach (var p in players)
		{
			p.Reset ();
			InitPlayer ();
			if (autoDraft)
				MoveCards (discard, fullDeckSize, deck);
			NextPlayer ();
		}
		currentPlayer = 0;
		mode = Mode.TitleScreen;
	}

	private void MoveCards (Deck from, int drawSize, Deck to)
	{
		for (int q = 0; q < drawSize && from.Count > 0; ++q)
			from.Top.Deck = to;
	}

	private void MoveMatchingCards (Deck from, int drawSize, int rarity, Deck to)
	{
		// They told use C#, they told it's nice, they told use foreach...
		// duplicating array to safely remove elements
		List<Card> newList = new List<Card>(from.Count);
		foreach (var c in from.Cards)
			newList.Add (c);
		foreach (var c in newList)
		{
			if (drawSize == 0)
				break;
			if (c.desc.rarity != rarity)
				continue;

			c.Deck = to;
			drawSize--;
			//q--
		}
	}

	private float scheduleNextPlayer = 0;
	private bool firstTurn = false;
	private int passedTurns = 0;
	void Update ()
	{
		if (mode == Mode.TitleScreen)
		{
			if (Input.GetKey("space") || Input.GetMouseButton(0))
			{
				if (background)
					background.material.mainTexture = bgTexture;
				mode = Mode.Draft;
			}

			return;
		}

		if (mode == Mode.Draft)
			infoText.text = "Draft! Player " + (currentPlayer+1).ToString();
		if (mode == Mode.Prepare)
			infoText.text = "Prepare for Rituals! Player " + (currentPlayer+1).ToString();
		else if (mode == Mode.GameOver)
			infoText.text = "Player " + (TopPlayer+1).ToString() + " Won!";
		else if (specialReplace > 0)
			infoText.text = "Replace " + specialReplace + " more ingridient" + ((specialReplace>1)?"s":"");
		else if (specialDiscard > 0)
			infoText.text = "Discard " + specialDiscard + " more rites" + ((specialDiscard>1)?"s":"") + " to continue";
		else
			infoText.text = "Player " + (currentPlayer+1).ToString();
		if (hand.Selected)
			infoText.text = "";

		if (scheduleNextPlayer <= 0)
		{
			scoreText.text = score.ToString();
			if ((int)mode < (int)Mode.BeginTurn)
				scoreText.text = "";
		}

		if (mode == Mode.Draft) // draft
		{
			firstTurn = true;
			discard.Visible = false;
			draft.Visible = true;
			pool.Visible = false;

			// Auto pick last card from booster and continue drafting from the new booster
			// RULE - Player A begins draft (picks first card from first booster).
			// When second BOOSTER opens, player B picks first (not player A again)
			if (draft.Count == 1 && scheduleNextPlayer <= 0)
				MoveCards (draft, 1, deck);

			if (draft.Count == 0 && discard.Count > 0)
			{
				MoveMatchingCards (discard, 4, 0, draft);
				MoveMatchingCards (discard, 3, 1, draft);
				MoveMatchingCards (discard, 2, 2, draft);
				MoveMatchingCards (discard, 1, 3, draft);
				MoveCards (discard, 4+3+2+1 - draft.Count, draft);
				draft.Shuffle ();
			}

			if (deck.Count == fullDeckSize)
				mode = Mode.EndDraft;
		}
		else if (mode == Mode.EndDraft)
		{
			deck.Shuffle ();
			MoveCards (deck, openingHandSize, hand);
			MoveCards (ingridients, poolSize - pool.Count, pool);

			EndDraft ();

			// do not switch players, if new booster was opened
			bool switchPlayers = discard.Count != 0 && draft.Count != 0 && draft.Count != boosterSize;
			if (switchPlayers)
				scheduleNextPlayer = 2.0f;
			mode = Mode.Prepare;
		}
		else if (mode == Mode.Prepare && (scheduleNextPlayer <= 0))
		{
			pool.Visible = true;
			mode = Mode.BeginTurn;
		}
		else if (mode == Mode.BeginTurn && (scheduleNextPlayer <= 0)) // begin turn
		{
			if (deck.Count == 0)
			{
				//score -= maxScore; // out of cards, you lost
				mode = Mode.GameOver; // game over
			}
			else
			{
				// RULE: - Player who plays first does not draw the 6th card (starts with 5)
				if (!firstTurn && hand.Count < maxHandSize)
					MoveCards (deck, 1, hand);
				firstTurn = false;

				// RULE: - When all players Pass turn and do not play any Rite cards, discard current ingridients and replace them
				if (passedTurns == playerCount)
				{
					MoveCards (pool, poolSize, ingridients);
					passedTurns = 0;
				}

				MoveCards (ingridients, poolSize - pool.Count, pool);

				endTurnButton.gameObject.SetActive (true);
				endTurnButton.interactable = true;

				mode = Mode.Turn;
			}
		}
		else if (mode == Mode.Turn) // turn
		{
			if (specialDiscard > 0) // ACTION: discard
				endTurnButton.interactable = false;
			else
			{
				if (!endTurnButton.interactable)
					endTurnButton.interactable = true;
				if (Input.GetKey("space"))
					OnEndTurnRequest ();
			}
			
			if (hand.Count == 0)
				mode = Mode.EndTurn;
		}
		else if (mode == Mode.EndTurn) // end turn
		{
			endTurnButton.gameObject.SetActive (false);
			endTurnButton.interactable = false;

			var turnScore = EndTurn (pool);
			score += turnScore;
			scoreText.text = "+" + turnScore.ToString() + " = " + score.ToString();

			if (score >= maxScore)
			{
				mode = Mode.GameOver; // game over, you won!
				NextPlayer ();
			}
			else
			{
				mode = Mode.BeginTurn;
				scheduleNextPlayer = 1.0f;
			}
		}
		else if (mode == Mode.GameOver) // game over!
		{
			MoveCards (pool, pool.Count, ingridients);
		}

		if (scheduleNextPlayer > 0)
		{
			scheduleNextPlayer -= Time.deltaTime;
			if (scheduleNextPlayer <= 0)
			{
				NextPlayer ();
				scheduleNextPlayer = 0;
			}
		}

	}

	public void OnEndTurnRequest ()
	{
		if (mode == Mode.Turn) // turn
		{
			if (specialDiscard <= 0) // ACTION: discard
			{
				hand.Selected = null;
				mode = Mode.EndTurn;
			}
		}
	}

	public void OnMouseEnter (Card card, Deck fromDeck)
	{
		if (mode == Mode.Draft) // draft
			if (fromDeck == draft)
				card.Deck.Selected = card;
	}

	public void OnMouseExit (Card card, Deck fromDeck)
	{
	}

	public void OnMouseDown (Card card, Deck fromDeck)
	{
		//bool scheduleNextPlayer = false;
		if (scheduleNextPlayer > 0)
			return;

		if (mode == Mode.Draft) // draft
		{
			card.Deck.Selected = null;
			if (fromDeck == draft)
			{
				card.Deck = deck;
				gameObject.GetComponent<AudioSource>().Play ();
				scheduleNextPlayer = .33f;
			}
		}
		else if (mode == Mode.Turn) // game
		{
			if (fromDeck == hand)
			{
				if (fromDeck.Selected == card)
					fromDeck.Selected = null;
				else
					fromDeck.Selected = card;

				if (specialDiscard > 0) // ACTION: 'specialDiscard'
				{
					card.Deck = discard;
					--specialDiscard;
				}
			}
			else if (fromDeck == pool)
			{
				bool rarityMatches = hand.Selected && hand.Selected.desc.rarity == card.desc.rarity;
				bool colorMatches = hand.Selected && (hand.Selected.desc.color == card.desc.color || hand.Selected.desc.color == -1 || card.desc.color == -1);
				int ritesInThePool = 0;
				bool canPlayRite = rarityMatches;
				foreach (var c in pool.Cards)
					if (c.HasSubDeck)
						ritesInThePool += c.SubDeck.Count;
				if (card.desc.action == "unique" && !colorMatches)
					canPlayRite = false;
				if (specialAnyRite) // ACTION: 'anyRite'
					canPlayRite = true;
				if (specialOnlyOneRite > 0 && ritesInThePool >= 1) // ACTION: 'riteOnly1'
					canPlayRite = false;
				if (hand.Selected && canPlayRite)
				{
					if (forcedAction == "")
						PlayAction (hand.Selected.desc.action);
					else
						PlayAction (forcedAction);
					hand.Selected.IsDragable = false;
					hand.Selected.Deck = card.SubDeck;
					hand.Selected = null;
					
					gameObject.GetComponent<AudioSource>().Play ();
				}

				if (specialReplace > 0)  // ACTION: 'specialReplace'
				{
					if (!card.HasSubDeck)
					{
						card.Deck = ingridients;
						MoveCards (ingridients, 1, pool);
						--specialReplace;
					}
				}
			}
		}
	}


	private void EndDraft ()
	{
		deck.transform.position = new Vector3(7.75f, deck.transform.position.y, deck.transform.position.z);

		hand.Visible = true;
		draft.Visible = false;
		discard.Visible = true;
		//if (scheduleNextPlayer <= 0)
		//	pool.Visible = true;
		deck.Visible = true;
		deck.cardSpacerX = .0f;
		deck.cardSpacerY = -.1f;
		deck.centered = false;
		deck.UpdateCards();
	}

	private int EndTurn (Deck pool)
	{
		int result = 0;

		var newList = new List<Card>(pool.Count);
		foreach (var c in pool.Cards)
			if (c.HasSubDeck)
				newList.Add (c);

		foreach (var c in newList)
		{
			foreach (var r in c.SubDeck.Cards)
			{
				bool colorBonus = c.desc.action == "common" && c.desc.color == r.desc.color;
				if (colorBonus)
					result += 1;
				result += r.desc.power;
				result += c.desc.power * specialMultiplier;
			}

			c.DiscardSubDeck (discard);
			c.Deck = ingridients;
		}

		if (specialOnlyOneRite > 0)
			specialOnlyOneRite--;

		if (result == 0) // no rites were played
			passedTurns++;
		else
			passedTurns = 0;

		PlayAction ("");
		return result;
	}


	[SerializeField]
	private bool specialAnyRite = false;
	[SerializeField]
	private int specialOnlyOneRite = 0;
	[SerializeField]
	private int specialReplace = 0;
	[SerializeField]
	private int specialMultiplier = 1;
	[SerializeField]
	private int specialDiscard = 0;

	private void PlayAction (string action)
	{
		specialAnyRite = false;
		// specialOnlyOneRite is decremented in EndTurn ()
		specialReplace = 0;
		specialMultiplier = 1;
		specialDiscard = 0;
		if (action == "draw1")
		{
			if (hand.Count < maxHandSize)
				MoveCards (deck, 1, hand);
			hand.UpdateCards ();
		}
		else if (action == "draw2")
		{
			if (hand.Count < maxHandSize)
				MoveCards (deck, 2, hand);
			hand.UpdateCards ();
		}
		else if (action == "riteAny")
		{
			specialAnyRite = true;
		}
		else if (action == "riteOnly1")
		{
			specialOnlyOneRite = playerCount; // AFFECTS OTHERS
		}
		else if (action == "replace2")
		{
			specialReplace = 2;
		}
		else if (action == "replace4")
		{
			specialReplace = 4;
		}
		else if (action == "replaceAny")
		{
			specialReplace = 99;
		}
		else if (action == "ingredientPowerX2")
		{
			specialMultiplier = 2;
		}
		else if (action == "discard1")
		{
			specialDiscard = 1;
		}
		else if (action == "discard2")
		{
			specialDiscard = 2;
		}
		else if (action == "everyoneDraw2")
		{
			foreach (var p in players)  // AFFECTS OTHERS
			{
				if (p.hand.Count < maxHandSize)
					MoveCards (p.deck, 2, p.hand);
				p.hand.UpdateCards ();
			}
		}
		else if (action == "othersDiscard1")
		{
			DiscardCards (1); // AFFECTS OTHERS
		}
		else if (action == "othersDiscard2")
		{
			DiscardCards (2); // AFFECTS OTHERS
		}

	}

	private void DiscardCards (int count)
	{
		for (int q = 0; q < count; ++q)
			for (int w = 1; w < playerCount; ++w) // skip yourself
			{
				var other = players[(currentPlayer+w)%playerCount];
				var card = other.hand.RandomCard();
				if (card)
					card.Deck = discard;
			}
	}


/*	void OnGUI ()
	{
		GUILayout.Label ("Player: " + currentPlayer.ToString());
		GUILayout.Label ("Score: " + score.ToString());

		if (mode == 2)
		{
			if (GUILayout.Button("Next"))
			{
				hand.Selected = null;
				mode = 3;
			}
		}
	}
	*/
}
