using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Json;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.IO;

namespace TriviaCrackHack
{
    internal class HttpCookieClient
    {
        private readonly CookieContainer container = new CookieContainer();

        public string UploadString(string url, string data = "")
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.CreateHttp(url);
            request.CookieContainer = container;

            if (data != String.Empty)
            {
                request.Method = "POST";

                byte[] binaryData = Encoding.ASCII.GetBytes(data);
                request.GetRequestStream().Write(binaryData, 0, binaryData.Length);
            }

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                ReadCookies(response);

                return new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (WebException ex)
            {
                WebResponse response = ex.Response;
                return new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
        }

        private void ReadCookies(WebResponse r)
        {
            var response = r as HttpWebResponse;
            if (response != null)
            {
                CookieCollection cookies = response.Cookies;
                container.Add(cookies);
            }
        }
    }

    internal class ServerResponse
    {
        public string RawResponse { private set; get; }
        public JsonObject RawObject { private set; get; }

        public string Message { private set; get; }
        public int Code { private set; get; }

        public ServerResponse(string response)
        {
            this.RawResponse = response;

            try
            {
                JsonValue jsonResponse = JsonValue.Parse(response);
                this.RawObject = jsonResponse.ToJsonObject();

                if (this.RawObject != null)
                {
                    if (this.RawObject.ContainsKey("message"))
                        this.Message = (string)this.RawObject["message"];
                    if (this.RawObject.ContainsKey("code"))
                        this.Code = (int)this.RawObject["code"];
                }
            }
            catch { }
        }
    }

    public class Question
    {

        public FullQuestion FullQuestion { private set; get; }

        public string Text { private set; get; }
        public string Category { private set; get; }
        public string MediaType { private set; get; }
        public int Id { private set; get; }
        public int CorrectAnswer { private set; get; }
        public bool PowerUpQuestion { private set; get; }
        public List<String> Answers { private set; get; }

        public Question(JsonValue jsonValue, FullQuestion fullQuestion)
        {
            this.FullQuestion = fullQuestion;

            this.Text = (string)jsonValue["text"];
            this.Category = (string)jsonValue["category"];
            this.MediaType = (string)jsonValue["media_type"];
            this.Id = (int)jsonValue["id"];
            this.CorrectAnswer = (int)jsonValue["correct_answer"];

            this.Answers = new List<string>();

            JsonArray answers = (JsonArray)jsonValue["answers"];
            foreach (JsonValue answer in answers)
                this.Answers.Add((string)answer);
        }

        public void SubmitAnswer(int answer)
        {
            FullQuestion.Spin.Game.CurrentUser.SubmitAnswer(this, answer);
        }
    }

    public class FullQuestion
    {
        public Spin Spin { private set; get; }
        public Question PowerupQuestion { private set; get; }
        public Question Question { private set; get; }

        public FullQuestion(JsonValue jsonValue, Spin spin)
        {
            this.Spin = spin;

            if (jsonValue.ContainsKey("powerup_question"))
                this.PowerupQuestion = new Question(jsonValue["powerup_question"], this);
            this.Question = new Question(jsonValue["question"], this);
        }
    }

    public class Spin
    {
        public Game Game { private set; get; }
        public List<FullQuestion> Questions { private set; get; }
        public string QuestionType { private set; get; }

        public Spin(JsonValue jsonValue, Game game)
        {
            this.Game = game;

            this.QuestionType = (string)jsonValue["type"];
            this.Questions = new List<FullQuestion>();

            JsonArray questionJsonArray = (JsonArray)jsonValue["questions"];
            foreach (JsonValue questionJsonValue in questionJsonArray)
                this.Questions.Add(new FullQuestion(questionJsonValue, this));
        }
    }

    public class Game
    {
        public string Status { private set; get; }
        public int Id { private set; get; }
        public int UnreadMessages { private set; get; }
        public int MyPlayerNumber { private set; get; }
        public int RoundNumber { private set; get; }
        public bool IsRandom { private set; get; }
        public bool Win { private set; get; }
        public bool MyTurn { private set; get; }

        public User Opponent { private set; get; }
        public User CurrentUser { private set; get; }

        public int Player1Charges { private set; get; }
        public int Player2Charges { private set; get; }

        public List<string> Player1Crowns { private set; get; }
        public List<string> Player2Crowns { private set; get; }

        public List<Spin> Spins { private set; get; }

        public Game(JsonValue jsonValue, User currentUser)
        {
            this.CurrentUser = currentUser;

            ReloadGame(jsonValue);
        }

        public void AcceptGame()
        {
            this.CurrentUser.AcceptGame(this);
        }

        internal void ReloadGame(JsonValue jsonValue)
        {
            this.Status = (string)jsonValue["game_status"];
            this.Id = (int)jsonValue["id"];
            this.UnreadMessages = (int)jsonValue["unread_messages"];
            this.MyPlayerNumber = (int)jsonValue["my_player_number"];
            this.RoundNumber = (int)jsonValue["round_number"];
            this.IsRandom = (bool)jsonValue["is_random"];
            this.MyTurn = (bool)jsonValue["my_turn"];

            this.Opponent = new User((string)jsonValue["opponent"]["username"], (int)jsonValue["opponent"]["id"]);

            if (jsonValue.ContainsKey("win"))
                this.Win = (bool)jsonValue["win"];

            this.Player1Charges = (int)jsonValue["player_one"]["charges"];
            this.Player1Crowns = new List<string>();
            if (jsonValue["player_one"].ContainsKey("crowns"))
            {
                JsonArray player1CrownsJsonArray = (JsonArray)jsonValue["player_one"]["crowns"];
                foreach (JsonValue crown in player1CrownsJsonArray)
                    this.Player1Crowns.Add((string)crown);
            }
            
            this.Player2Charges = (int)jsonValue["player_two"]["charges"];
            this.Player2Crowns = new List<string>();
            if (jsonValue["player_two"].ContainsKey("crowns"))
            {
                JsonArray player2CrownsJsonArray = (JsonArray)jsonValue["player_two"]["crowns"];
                foreach (JsonValue crown in player2CrownsJsonArray)
                    this.Player2Crowns.Add((string)crown);
            }

            this.Spins = new List<Spin>();
            if (jsonValue.ContainsKey("spins_data"))
            {
                JsonArray spinsJsonArray = (JsonArray)jsonValue["spins_data"]["spins"];
                foreach (JsonValue spinJsonValue in spinsJsonArray)
                    this.Spins.Add(new Spin(spinJsonValue, this));
            }
        }
    }

    public class User
    {
        private const String ANSWER_URL = "http://api.preguntados.com/api/users/{0}/games/{1}/answers";
        private const String GAME_URL = "http://api.preguntados.com/api/users/{0}/games/{1}?";
        private const String LOGIN_URL = "https://api.preguntados.com/api/login";
        private const String DASHBOARD_URL = "http://api.preguntados.com/api/users/{0}/dashboard";
        private const String NEW_GAME_URL = "http://api.preguntados.com/api/users/{0}/games";

        private HttpCookieClient client = new HttpCookieClient();
        private string password;

        public string Username { private set; get; }
        public string Email { private set; get; }
        public int Coins { private set; get; }
        public int Id { private set; get; }
        public List<Game> Games { private set; get; }

        public DateTime NextLife { private set; get; }
        public int MaxLives { private set; get; }
        public int Lives { private set; get; }

        public User(string email, string password)
        {
            this.Games = new List<Game>();

            this.Email = email;
            this.password = password;
        }

        public User(string username, int id)
        {
            this.Games = new List<Game>();

            this.Username = username;
            this.Id = id;
        }

        public bool Login()
        {
            var data = new Dictionary<object, object>
                {
                    { "password", this.password },
                    { "email", this.Email },
                    { "language", "en" }
                };

            ServerResponse response = this.GetData(LOGIN_URL, data);
            if (response.Message != null)
            {
                Console.WriteLine("Server Error: Message={0}, Code={1}", response.Message, response.Code);
            }
            else
            {
                JsonObject jsonObject = response.RawObject;
                if (jsonObject != null)
                {
                    this.Username = (string)jsonObject["username"];
                    this.Email = (string)jsonObject["email"];
                    this.Coins = (int)jsonObject["coins"];
                    this.Id = (int)jsonObject["id"];

                    return true;
                }
            }

            return false;
        }

        public void Refresh()
        {
            ServerResponse response = this.GetData(String.Format(DASHBOARD_URL, this.Id));
            if (response.Message != null)
            {
                Console.WriteLine("Server Error: Message={0}, Code={1}", response.Message, response.Code);
            }
            else
            {
                JsonObject jsonObject = response.RawObject;
                if (jsonObject != null)
                {
                    this.Coins = (int)jsonObject["coins"];
                    this.Lives = (int)jsonObject["lives"]["quantity"];
                    this.MaxLives = (int)jsonObject["lives"]["max"];

                    if (jsonObject["lives"].ContainsKey("next_increment"))
                        this.NextLife = DateTime.Now.AddSeconds((int)jsonObject["lives"]["next_increment"]);
                    else
                        this.NextLife = DateTime.MinValue;

                    this.Games.Clear();
                    JsonArray gamesJsonArray = (JsonArray)jsonObject["list"];
                    foreach (JsonValue gameJsonValue in gamesJsonArray)
                        this.Games.Add(new Game(gameJsonValue, this));
                }
                else
                {
                    Console.WriteLine("Unexpected Response: {0}", response.RawResponse);
                }
            }
        }

        public void SubmitAnswer(Question question, int answer)
        {
            var final = new Dictionary<object, object>
                {
                    { "type", question.FullQuestion.Spin.QuestionType },
                    { 
                        "answers", new object[]
                                    { 
                                        new Dictionary<string, object>
                                            { 
                                                { "id", question.Id.ToString() },
                                                { "answer", answer.ToString() },
                                                { "category", question.Category }
                                    
                                            }
                                    }
                    }
                };

            Game game = question.FullQuestion.Spin.Game;

            ServerResponse response = this.GetData(String.Format(ANSWER_URL, this.Id, game.Id), final);
            if (response.Message != null)
            {
                Console.WriteLine("Server Error: Message={0}, Code={1}", response.Message, response.Code);
            }
            else
            {
                JsonObject gameData = response.RawObject;
                game.ReloadGame(gameData);
            }
        }

        public Game CreateGame(string username = null)
        {
            var data = new Dictionary<object, object>
                {
                    { "language", "en" },
                };

            if (username != null)
                data.Add("opponent", new Dictionary<object, object> { { "username", username } });

            ServerResponse response = this.GetData(String.Format(NEW_GAME_URL, this.Id), data);
            if (response.Message != null)
            {
                Console.WriteLine("Server Error: Message={0}, Code={1}", response.Message, response.Code);
            }
            else
            {
                JsonValue jsonValue = response.RawObject;
                if (jsonValue != null)
                {
                    Game game = new Game(jsonValue, this);
                    this.Games.Add(game);
                    return game;
                }
            }

            return null;
        }

        public void AcceptGame(Game game)
        {
            ServerResponse response = this.GetData(String.Format(GAME_URL, this.Id, game.Id));
            if (response.Message != null)
            {
                Console.WriteLine("Server Error: Message={0}, Code={1}", response.Message, response.Code);
            }
            else
            {
                JsonObject gameData = response.RawObject;
                game.ReloadGame(gameData);
            }
        }

        private ServerResponse GetData(string url, Dictionary<object, object> data)
        {
            string jsonData = new JavaScriptSerializer().Serialize(data);
            return new ServerResponse(this.client.UploadString(url, jsonData));
        }

        private ServerResponse GetData(String url)
        {
            return new ServerResponse(this.client.UploadString(url));
        }
    }

    public class Program
    {
        static void Main()
        {
            User user = new User("user@email.com", "password");
            if (user.Login())
            {
                user.Refresh();

                foreach (Game game in user.Games)
                {
                    if (game.MyPlayerNumber == 1)
                        Console.WriteLine("{0} ({1}) vs {2} ({3}), {4}", game.CurrentUser.Username, game.Player1Crowns.Count, game.Opponent.Username, game.Player2Crowns.Count, game.Status);
                    else
                        Console.WriteLine("{0} ({1}) vs {2} ({3}), {4}", game.CurrentUser.Username, game.Player2Crowns.Count, game.Opponent.Username, game.Player1Crowns.Count, game.Status);
                }
            }

            Console.ReadLine();
        }
    }
}
