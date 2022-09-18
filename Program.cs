var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDeckOfCards, DeckOfCards>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Gets a random card from the deck, without modifying the deck.
app.MapGet("/deck/card/random", (IDeckOfCards deck) =>
{
    var card = deck.RandomCard();
    if (card is not null) return Results.Ok(new Message(card.Text, card, deck));
    else return Results.NotFound(new Message("Deck is empty", card, deck));
});

// Gets the next card from the deck, without modifying the deck.
app.MapGet("/deck/card/next", (IDeckOfCards deck) =>
{
    var card = deck.NextCard();
    if (card is not null) return Results.Ok(new Message(card.Text, card, deck));
    else return Results.NotFound(new Message("Deck is empty", card, deck));
});

// Gets a specific card from the deck, without modifying the deck.
app.MapGet("/deck/card/", (CardNumber number, CardSuit suit, IDeckOfCards deck) =>
{
    var card = deck.Cards.Find(c => c.Equals(new Card(number, suit)));
    if (card is not null) return Results.Ok(new Message(card.Text, card, deck));
    else return Results.NotFound(new Message("Deck is empty", card, deck));
});

// Gets all the cards in the deck.
app.MapGet("/deck", (IDeckOfCards deck) =>
{
    return deck;
});

// Draws a random card from the deck, modifying the deck.
app.MapPut("/deck/card/random", (IDeckOfCards deck) =>
{
    var card = deck.RandomCard();
    if (card is not null)
    {
        deck.RemoveCard(card);
        return Results.Ok(new Message(card.Text, card, deck));
    }
    else return Results.NotFound(new Message("Deck is empty", card, deck));
});

// Draws the next card from the deck, modifying the deck.
app.MapPut("/deck/card/next", (IDeckOfCards deck) =>
{
    var card = deck.NextCard();
    if (card is not null)
    {
        deck.RemoveCard(card);
        return Results.Ok(new Message(card.Text, card, deck));
    }
    else return Results.NotFound(new Message("Deck is empty", card, deck));
});

// Draws a specific card from the deck, modifying the deck.
app.MapPut("/deck/card", (Card card, IDeckOfCards deck) =>
{
    if ((card.Suit == CardSuit.Joker && deck.Cards.Where(c => c.Equals(card)).Count() >= 3)
       || !deck.Cards.Contains(card))
    {
        deck.AddCard(card);
        return Results.Ok(new Message(card.Text, card, deck));
    }
    return Results.UnprocessableEntity(new Message($"Cannot add {card.ToString()} to deck, already present (or 3 are present if joker)", card, deck));
});

// Shuffles the content of the deck, builds a new deck first if empty.
app.MapPost("/deck/shuffle", (IDeckOfCards deck) =>
{
    return deck.Shuffle();
});

// Builds a new unshuffled deck.
app.MapPost("deck/reset", (IDeckOfCards deck) =>
{
    return deck.ResetOrInitialize();
});

app.Run();


/// <summary>
/// A record of a playing card from ace to king, of the four standard card suits. Alternatively is a joker card.
/// </summary>
/// <param name="Number">0 = joker, 1-13 is ace through king</param>
/// <param name="Suit">0 = joker, 1-4 is heart through spade</param>
public record Card (CardNumber Number, CardSuit Suit)
{
    public string Text { get => ToString(); }
    /// <summary>
    /// A card is valid if it fits into the enums <c>CardNumber</c> and <c>CardSuit</c>.
    /// </summary>
    /// <returns>True if the card is valid</returns>
    public bool IsValid()
    {
        return !(Number < CardNumber.Joker || Number > CardNumber.King ||
                 Suit < CardSuit.Joker || Suit > CardSuit.Spade);
    }

    /// <summary>
    /// Natural text representing a card.
    /// </summary>
    /// <returns>The text of the card number and suit</returns>
    public override string ToString()
    {
        if (Number == CardNumber.Joker || Suit == CardSuit.Joker) return $"Joker";
        else if (Number < CardNumber.Ace || Number > CardNumber.King ||
            Suit < CardSuit.Heart || Suit > CardSuit.Spade) return $"Illegal card";
        else return $"{Number} of {Suit}s";
    }
}
public enum CardSuit { Joker, Heart, Diamond, Club, Spade };
public enum CardNumber { Joker, Ace, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King };

/// <summary>
/// A deck of standard playing cards with methods to shuffle and draw from it.
/// </summary>
public interface IDeckOfCards
{
    /// <summary>
    /// A list of the current cards in the deck.
    /// </summary>
    List<Card> Cards { get; }
    /// <summary>
    /// A count of the number of cards in deck, repeated to aid in serialization.
    /// </summary>
    int Count { get; }
    /// <summary>
    /// Add a card to the deck. Validation of this is legal is expected to happen elsewhere.
    /// </summary>
    /// <param name="card">The card to add</param>
    void AddCard(Card card);
    /// <summary>
    /// The first card in the deck or null if the list of cards is empty.
    /// </summary>
    /// <returns>The first card in the deck or null if the list of cards is empty</returns>
    Card? NextCard();
    /// <summary>
    /// A random card from the deck or null if the list of cards is empty.
    /// </summary>
    /// <returns>A random card from the deck or null if the list of cards is empty</returns>
    Card? RandomCard();
    /// <summary>
    /// Remove a specific card from the deck.
    /// </summary>
    /// <param name="card">The card to remove</param>
    /// <returns>True if the card was removed, false if was not</returns>
    bool RemoveCard(Card card);
    /// <summary>
    /// Build a new unshuffled deck. Will have four suits and three jokers.
    /// </summary>
    /// <returns>The new list of cards</returns>
    List<Card> ResetOrInitialize();
    /// <summary>
    /// Shuffle a deck, invokes <c>ResetOrInitialize()</c> if the deck is currently empty.
    /// </summary>
    /// <returns>The shuffled list of cards</returns>
    List<Card> Shuffle();
}

public class DeckOfCards : IDeckOfCards
{
    private List<Card> cards;
    public List<Card> Cards { get => cards; }
    public int Count {  get => cards.Count; }
    private Random rng;

    /// <summary>
    /// Constructor for the deck of cards, invokes <c>Shuffle()</c>.
    /// </summary>
    public DeckOfCards()
    {
        rng = new();
        cards = Shuffle();
    }

    /// <inheritdoc />
    public List<Card> ResetOrInitialize()
    {
        cards = new List<Card>();
        for (CardNumber n = CardNumber.Ace; n <= CardNumber.King; n++)
        {
            for (CardSuit c = CardSuit.Heart; c <= CardSuit.Spade; c++)
            {
                cards.Add(new Card(n, c));
            }
        }
        cards.Add(new Card(CardNumber.Joker, CardSuit.Joker));
        cards.Add(new Card(CardNumber.Joker, CardSuit.Joker));
        cards.Add(new Card(CardNumber.Joker, CardSuit.Joker));
        return cards;
    }

    /// <inheritdoc />
    public List<Card> Shuffle()
    {
        Random rng = new Random();
        if (cards is null || cards.Count == 0) cards = ResetOrInitialize();
        var shuffledDeck = cards.OrderBy(a => rng.Next()).ToList();
        cards = shuffledDeck;
        return cards;
    }

    /// <inheritdoc />
    public Card? NextCard()
    {
        if (cards.Count == 0) return null;
        return cards.First();
    }

    /// <inheritdoc />
    public Card? RandomCard()
    {
        if (cards.Count == 0) return null;
        return cards[rng.Next(cards.Count)];
    }

    /// <inheritdoc />
    public bool RemoveCard(Card card)
    {
        return cards.Remove(card);
    }

    /// <inheritdoc />
    public void AddCard(Card card)
    {
        cards.Add(card);
    }
}

public record Message (string message, Card? card, IDeckOfCards? deck);