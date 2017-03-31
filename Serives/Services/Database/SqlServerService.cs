using System;

namespace GameServices
{
    using System.Collections.Generic;
    using System.Data.SqlClient;

    //public enum ConnectResult
    //{
    //    Success,
    //    ConnectFail,
    //    EmptyUserNameOrPsw,
    //    UserExist,
    //    UserNotExist,
    //    WrongPassword,
    //    GenerateRecordFail,
    //    OtherError
    //}

    internal class SqlServerService
    {
        public bool HasConnected { get; private set; }
        public bool CanConnect => OnlineService.CheckCanConnect("bds256164280.my3w.com");
        public OnlineAccount LoginedAccount { get; private set; }
        static string connectionString = Properties.Settings.Default.SqlServerConnectionString;

        private SqlServerService()
        {
        }

        private static SqlServerService sqlServerService;
        private static object lockhelper = new object();
        public static SqlServerService GetService()
        {
            lock (lockhelper)
            {
                if (sqlServerService == null) sqlServerService = new SqlServerService();
                return sqlServerService;
            }
        }

        internal SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
        
        /// <summary>
        /// Generate hash string with salt
        /// </summary>
        /// <param name="password">password</param>
        /// <param name="salt">salt</param>
        /// <returns></returns>
        private string GenerateHashString(string password, string salt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(salt)) return string.Empty;
            byte[] passwordAndSaltBytes = System.Text.Encoding.UTF8.GetBytes(password + salt);
            byte[] hashBytes = new System.Security.Cryptography.SHA256Managed().ComputeHash(passwordAndSaltBytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Register a new account on online server
        /// </summary>
        /// <param name="name">Show name</param>
        /// <param name="username">Login name</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        public ConnectResult Register(string nickName, string userName, string password, out string errorMsg)
        {
            if (!CanConnect)
            {
                errorMsg = "Can't connect to network";
                return ConnectResult.ConnectFail;
            }
            errorMsg = string.Empty;
            Logout();
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    string selectString = $"select count(*) as count from epx_users where username = '{userName}';";
                    var command = new SqlCommand(selectString, conn);
                    conn.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        int count = Convert.ToInt32(reader["count"]);
                        if (count > 0)
                        {
                            errorMsg = "Account exists!";
                            return ConnectResult.UserExist;
                        }
                    }
                    reader.Close();
                    //Generate hash string and salt
                    string salt = Guid.NewGuid().ToString();
                    string hashString = GenerateHashString(password, salt);
                    //Execute query
                    string sqlString = $"insert into epx_users (nickname, username, password, salt) values('{nickName}', '{userName}', '{hashString}', '{salt}')";
                    SqlCommand cmd = new SqlCommand(sqlString, conn);
                    int result = cmd.ExecuteNonQuery();
                    GameBasic.Debug.Log($"Result effect is {result}");
                }
            }
            catch (Exception e)
            {
                GameBasic.Debug.Log($"StackTrace:\n {e.StackTrace} \nMessage:\n{e.Message}");
                errorMsg = e.Message;
                return ConnectResult.OtherError;
            }
            return Login(userName, password, out errorMsg);
        }

        /// <summary>
        /// Login to online server
        /// </summary>
        /// <param name="userName">User name</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        public ConnectResult Login(string userName, string password, out string errorMsg)
        {
            //Check data
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(userName))
            {
                errorMsg = "Empty userName/password!";
                return ConnectResult.EmptyUserNameOrPsw;
            }
            if (!CanConnect)
            {
                errorMsg = "Can't connect to network";
                return ConnectResult.ConnectFail;
            }
            HasConnected = false;
            LoginedAccount = null;
            OnlineAccount account = null;
            GameBasic.GameScore score = null;
            errorMsg = string.Empty;
            try
            {
                //Get salt and verify if user exist.
                string salt = string.Empty;
                using (SqlConnection conn = GetConnection())
                {
                    SqlCommand cmd = new SqlCommand($"select * from epx_users where username = '{userName}'", conn);
                    conn.Open();
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        salt = Convert.ToString(reader["salt"]);
                    }
                }
                if (string.IsNullOrEmpty(salt))
                {
                    errorMsg = "User not exist!";
                    return ConnectResult.UserNotExist;
                }
                //Verify password
                string hashString = GenerateHashString(password, salt);
                ExecuteReader($"select * from epx_users where username = '{userName}' and password = '{hashString}';", reader =>
                 {
                     while (reader.Read())
                     {
                         account = new OnlineAccount();
                         account.State = AccountState.Online;
                         account.UserName = userName;
                         account.UserID = Convert.ToInt32(reader["id"]);
                         account.NickName = reader["nickname"].ToString();
                     }
                 });
                //Check if login successed
                if (account == null)
                {
                    errorMsg = "Wrong password";
                    return ConnectResult.WrongPassword;
                }
                //Check record in scores
                ExecuteReader($"select * from epx_scores where userid = '{account.UserID}';", reader =>
                {
                    while (reader.Read())
                    {
                        score = new GameBasic.GameScore()
                        {
                            UserID = account.UserID,
                            NickName = Convert.ToString(reader["nickname"]),
                            Score = Convert.ToInt32(reader["score"]),
                            Win = Convert.ToInt32(reader["win"]),
                            Fail = Convert.ToInt32(reader["fail"]),
                            Drawn = Convert.ToInt32(reader["drawn"]),
                            ModifyTime = Convert.ToDateTime(reader["modifytime"])
                        };
                    }
                });
                //If record zero, create it
                if (score == null)
                {
                    GameBasic.Debug.Log($"score is null");
                    var newscore = new GameBasic.GameScore()
                    {
                        UserID = account.UserID,
                        NickName = account.NickName,
                        ModifyTime = DateTime.Now
                    };
                    if (ExecuteNonQuery(newscore.GetInsert("epx_scores")) > 0)
                    {
                        score = newscore;
                        GameBasic.Debug.Log($"Insert record successed");
                    }
                    else
                    {
                        GameBasic.Debug.Log($"Insert record to scores fail");
                        errorMsg = "Insert record to scores fail";
                        return ConnectResult.GenerateRecordFail;
                    }
                }
            }
            catch (Exception e)
            {
                GameBasic.Debug.Log($"StackTrace:\n {e.StackTrace} \nMessage:\n{e.Message}");
                errorMsg = e.Message;
                return ConnectResult.OtherError;
            }
            if (score != null)
            {
                LoginedAccount = account;
                LoginedAccount.Score = score;
                OnLoginSuccessed();
                return ConnectResult.Success;
            }
            return ConnectResult.OtherError;
        }

        /// <summary>
        /// Release Account
        /// </summary>
        public void Logout()
        {
            LoginedAccount = null;
            HasConnected = false;
        }

        async void OnLoginSuccessed()
        {
            HasConnected = true;
            await System.Threading.Tasks.Task.Run(() =>
            {
                TrySync();
            });
        }

        public GameBasic.GameScore TrySync()
        {
            if (this.LoginedAccount == null) return null;
            bool onlineNewest;
            var sqliteserver = SqliteService.GetService();
            var sqlserver = SqlServerService.GetService();
            var score = sqliteserver.QueryScore($"where userid={sqlserver.LoginedAccount.UserID}");
            string cmdString = sqlserver.SyncScore(score, out onlineNewest);
            if (onlineNewest)
            {
                sqliteserver.ExecuteNonQuery(cmdString);
            }
            return sqliteserver.QueryScore($"where userid={sqlserver.LoginedAccount.UserID}");
        }

        /// <summary>
        /// Try to sync to online database
        /// </summary>
        /// <param name="localScore">the local score</param>
        /// <param name="onlineNewest">if is the online newest</param>
        /// <returns>Return command string of update to local</returns>
        internal string SyncScore(GameBasic.GameScore localScore, out bool onlineNewest)
        {
            onlineNewest = false;
            string localQuery = string.Empty;
            if (localScore != null && localScore.UserID != LoginedAccount.UserID) return string.Empty;
            //List<GameBasic.GameScore> scores = new List<GameBasic.GameScore>();
            GameBasic.GameScore onlineScore = null;
            var userid = LoginedAccount.UserID;
            using (SqlConnection conn = GetConnection())
            {
                SqlCommand cmd = new SqlCommand($"select * from epx_scores where userid = '{userid}';", conn);
                conn.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    onlineScore = new GameBasic.GameScore()
                    {
                        UserID = userid,
                        NickName = Convert.ToString("nickname"),
                        Score = Convert.ToInt32(reader["score"]),
                        Win = Convert.ToInt32(reader["win"]),
                        Fail = Convert.ToInt32(reader["fail"]),
                        Drawn = Convert.ToInt32(reader["drawn"]),
                        ModifyTime = Convert.ToDateTime(reader["modifytime"])
                    };
                }
                //verify modify time
                //If online newest, update to local
                //If local newest, update to online
                if (localScore != null)
                {
                    //If local score is not null, check the local score id
                    if (localScore.ModifyTime > onlineScore.ModifyTime)
                    {
                        var insert = ExecuteNonQuery(onlineScore.GetUpdate(localScore, "epx_scores", $"where userid={userid}"));
                        GameBasic.Debug.LogLine($"Update result is {insert}");
                    }
                    else if (localScore.ModifyTime < onlineScore.ModifyTime)
                    {
                        onlineNewest = true;
                        localQuery = localScore.GetUpdate(onlineScore, "scores");
                    }
                }
                else
                {
                    if (onlineScore != null)
                    {
                        localQuery = onlineScore.GetInsert("scores");
                    }
                }
            }
            LoginedAccount.Score = onlineNewest ? onlineScore : localScore;
            return localQuery;
        }

        public IList<GameBasic.GameScore> GetTop(int num)
        {
            List<GameBasic.GameScore> gamescores = new List<GameBasic.GameScore>();
            try
            {
                ExecuteReader($"select top {num}*  from epx_scores order by score desc; ", reader =>
                {
                    while (reader.Read())
                    {
                        gamescores.Add(new GameBasic.GameScore()
                        {
                            ScoreID = Convert.ToInt32(reader["scoreid"]),
                            UserID = Convert.ToInt32(reader["userid"]),
                            NickName = reader["nickname"].ToString(),
                            Score = Convert.ToInt32(reader["score"]),
                            Win = Convert.ToInt32(reader["win"]),
                            Fail = Convert.ToInt32(reader["fail"]),
                            Drawn = Convert.ToInt32(reader["drawn"]),
                            ModifyTime = Convert.ToDateTime(reader["modifytime"])
                        });
                    }
                });
            }catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
            return gamescores;
        }

        internal object ExecuteScalar(string cmdString)
        {
            if (string.IsNullOrEmpty(cmdString)) return 0;
            GameBasic.Debug.LogLine(cmdString);
            using (SqlConnection conn = GetConnection())
            {
                SqlCommand cmd = new SqlCommand(cmdString, conn);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        internal int ExecuteNonQuery(string cmdString)
        {
            if (string.IsNullOrEmpty(cmdString)) return 0;
            GameBasic.Debug.LogLine(cmdString);
            using (SqlConnection conn = GetConnection())
            {
                SqlCommand cmd = new SqlCommand(cmdString, conn);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }
        internal void ExecuteReader(string cmdString, Action<SqlDataReader> action)
        {
            if (string.IsNullOrEmpty(cmdString)) return;
            GameBasic.Debug.LogLine(cmdString);
            using (SqlConnection conn = GetConnection())
            {
                SqlCommand cmd = new SqlCommand(cmdString, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();
                action.Invoke(reader);
            }
        }

    }
}
