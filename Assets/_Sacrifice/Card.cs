using UnityEngine;
using System.Collections;
using System;
using System.Linq;

    [Serializable]
    public struct CardDesc
    {
        public int color;
        public int rarity;
        public int power;
        public string action; // 16 different
        public Texture texture;
    };


    public class Card : MonoBehaviour
    {
        public CardDesc desc;

        private Deck deck;
        public Deck Deck
        {
            get
            {
                return deck;
            }
            set
            {
                if (deck != value)
                {
                    if (deck != null)
                    {
                        deck.Cards.Remove(this);
                        deck.Selected = null;
                    }
                    deck = value;
                    deck.Cards.Add(this);

                    Deck.UpdateCards();
                }
            }
        }

        private Deck subDeck = null;
        public Deck SubDeck
        {
            get
            {
                if (!HasSubDeck)
                {
                    subDeck = (Instantiate(Game.Instance.ingridientDeckPrefab) as GameObject).GetComponent<Deck>();
                    subDeck.order = deck.order-1;
                    subDeck.transform.parent = transform;
                    subDeck.transform.localPosition = Vector3.zero + new Vector3(subDeck.cardSpacerX, subDeck.cardSpacerY) * .4f;
                    //ingridientDeck.maxCardsSpace = 10;
                    //ingridientDeck.cardSpacerX = 0.05f;
                    //ingridientDeck.cardSpacerY = 0.25f;
                }
                
                return subDeck;
            }
        }
        public bool HasSubDeck { get { return subDeck != null; } }
        public void DiscardSubDeck (Deck discard)
        {
            if (subDeck == null)
                return;

            for (int q = 0; q < subDeck.Count; ++q)
                subDeck.Cards[q].Deck = discard;

            Destroy (subDeck);
            subDeck = null;
        }

        bool visible = true;
        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        bool isDragable = true;
        public bool IsDragable
        {
            get { return isDragable; }
            set { isDragable = value; }
        }

        bool isDragging = false;
        Vector3 clickPosition;
        float doubleClickStart = 0;
        bool dragDisabled;

        public void Start()
        {
            var particleSystem = GetComponentInChildren<ParticleSystem>();
            if (particleSystem != null)
                particleSystem.GetComponent<Renderer>().sortingLayerName = "Foreground"; // see http://answers.unity3d.com/questions/577288/particle-system-rendering-behind-sprites.html
        }

        void OnMouseEnter()
        {
            Game.Instance.OnMouseEnter(this, deck);
        }

        void OnMouseExit()
        {
            Game.Instance.OnMouseExit(this, deck);
        }

        void OnMouseDown()
        {
            Game.Instance.OnMouseDown(this, deck);
        }

        void OnMouseUp()
        {
        }
/*
        public void SetDeck(int newPosition, Deck deck = null)
        {
            Deck.Cards.Remove(this);
            if (deck != null)
            {
                Deck.UpdateCards();
                this.deck = deck;
            }
            Deck.Cards.Insert(newPosition, this);
            Deck.UpdateCards();
        }
        */
    }
