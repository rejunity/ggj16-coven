// #define NGUI // Uncomment this to use NGUI tweens instead of iTween

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;


    public class Deck : MonoBehaviour
    {
        public int order = 0;
        public int sortStep = 1;
        public bool centered = true;
        List<Card> cards = new List<Card>();
        public List<Card> Cards { get { return cards; } }
        public int Count { get { return cards.Count; } }
        public Card Top { get { return cards[0]; } }

        public bool Visible
        {
            get
            {
                return gameObject.activeSelf;
            }
            set
            {
                gameObject.SetActive (value);
                UpdatesDisabled = !value;
                UpdateCards();
            }
        }

        Card selected = null;
        public Card Selected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
                UpdateCards();
            }
        }
/*
        public void CreateCards(int count = 8)
        {
            UpdatesDisabled = true;
            for (int q = 1; q <= count; q++)
            {
                var card = (Instantiate(Game.Instance.cardPrefab) as GameObject).GetComponent<Card>();
                card.transform.localScale = Game.Instance.cardPrefab.transform.localScale;
                card.desc = Game.Instance.cards[q % Game.Instance.cards.Length];
                card.Deck = this;
                //cards.Add(card);
            }
            UpdatesDisabled = false;
            UpdateCards();
        }
*/
        public void CreateCards(CardDesc[] descs, int repeat = 1)
        {
            UpdatesDisabled = true;
            foreach (var desc in descs)
            {
                for (int q = 0; q < repeat; ++q)
                {
                    for (int w = 0; w < (int)Mathf.Max(1, 4 - desc.rarity); ++w)
                    {
                        var card = (Instantiate(Game.Instance.cardPrefab) as GameObject).GetComponent<Card>();
                        card.transform.localScale = Game.Instance.cardPrefab.transform.localScale;
                        card.desc = desc;
                        card.Deck = this;
                    }
                }
            }
            UpdatesDisabled = false;
            UpdateCards();
        }


        static System.Random random = new System.Random();
        public void Shuffle()
        {
            Shuffle(7, random);
        }
        public void Shuffle(int times, System.Random random)
        {
            for (int time = 0; time < times; time++)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    var newIndex = random.Next(0, cards.Count);
                    cards.Remove(card);
                    cards.Insert(newIndex, card);
                }
            }
        }
        public Card RandomCard()
        {
            if (cards.Count <= 0)
                return null;
            return cards[random.Next(0, cards.Count)];
        }

        public float cardSpacerX = 1.25f;
        public float cardSpacerY = 0;
        public int maxCardsSpace = 8;


        public bool UpdatesDisabled = false;

        public void UpdateCards()
        {
            if (!Visible)
                foreach (var c in cards)
                    c.transform.parent = transform;

            if (UpdatesDisabled)
                return;

            //print("Updating cards on deck " + (TopCard == null ? "Empty" : TopCard.ToString()));

            var localCardSpacerX = cardSpacerX;
            var localCardSpacerY = cardSpacerY;

            if ((maxCardsSpace > 0) && (cards.Count > maxCardsSpace))
            {
                //override the spacers values to squeeze cards
                localCardSpacerX = (cardSpacerX * maxCardsSpace) / cards.Count;
                localCardSpacerY = (cardSpacerY * maxCardsSpace) / cards.Count;
            }

            //Loop on the Deck Cards (not playing cards)
            var lastTransform = transform;
            for (int i = 0; i < cards.Count; i++)
            {
                //Get the card object
                var card = cards[i];
                //var spriteRenderer = card.GetComponent<SpriteRenderer>();
                //if (spriteRenderer)
                //    spriteRenderer.sprite = card.desc.sprite;
                var meshRenderer = card.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    MaterialPropertyBlock mpb = new MaterialPropertyBlock ();
                    //mpb.SetTexture ("_DetailAlbedoMap", card.desc.texture);
                    mpb.SetTexture ("_MainTex", card.desc.texture);
                    meshRenderer.SetPropertyBlock (mpb);
                }
                card.transform.parent = lastTransform;
                //lastTransform = card.transform;

                var localSortOrder = sortStep*(i+1) + ((selected == card)?cards.Count:0);
                var targetLocalPosition = new Vector3(0, 0, (-localSortOrder) * 0.03f); // z needs to be set for mouse hit detection
                targetLocalPosition += new Vector3(localCardSpacerX, localCardSpacerY) * ((float)i - (centered?1:0) * cards.Count/2.0f);

                var targetLocalScale = Game.Instance.cardPrefab.transform.localScale;
                if (selected == card)
                {
                    targetLocalScale = targetLocalScale * 1.75f;
                    targetLocalPosition -= Vector3.forward * 0.5f;
                }

#if NGUI
                TweenPosition.Begin(card.gameObject, 0.25f, targetLocalPosition);
                TweenSize.Begin(card.gameObject, 0.25f, targetLocalScale);
#else
                iTween.MoveTo(card.gameObject, iTween.Hash("position", targetLocalPosition, "islocal", true, "time", 0.25f));
                iTween.ScaleTo(card.gameObject, iTween.Hash("scale", targetLocalScale, "islocal", true, "time", 0.25f));
#endif
                card.GetComponent<Collider2D>().enabled = false; // disable the collider until the card is finished moving.

                var sortOrder = order + localSortOrder; // Right decks are on top of left decks when squeezing together
                card.GetComponent<Renderer>().sortingOrder = sortOrder; // sort order needs to be set for visual to render correctly
                StartCoroutine(Delay(() => // schedule this for when the card finishes moving
                {
                    card.GetComponent<Collider2D>().enabled = card.IsDragable;
                }, 0.25f));
            }
        }
        
        public static IEnumerator Delay(Action action, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        void OnMouseEnter()
        {
            Debug.Log ("OnMouseEnter");
        }
    }