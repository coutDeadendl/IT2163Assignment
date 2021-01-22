using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace _164362Q_Assignment1 {
    public partial class UserProfile : System.Web.UI.Page {
        string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SITConnectDB"].ConnectionString;
        byte[] Key;
        byte[] IV;
        byte[] cc = null;
        string finalHash;
        string pwSuggestion = "";

        protected void Page_Load(object sender, EventArgs e){
            // signup button disabled until password passes client check.
            btn_submit.Enabled = false;
            // verify session
            if (Session["UserId"] != null && Session["AuthToken"] != null && Request.Cookies["AuthToken"] != null) {
                if (!Session["AuthToken"].ToString().Equals(Request.Cookies["AuthToken"].Value)) {
                    Response.Redirect("Login.aspx?src=" + "timeout", false);
                }
                else {
                    if (Session["UserId"] != null) {
                        string id = (string)Session["UserId"];
                        loadUser(id);
                        if (Request.QueryString["src"] == "update") {
                            updateSuccess.Visible = true;
                        }

                        if (passwordAge((string)Session["UserId"]) > 15) {
                            btn_logout.Enabled = false;
                            // user cannot log out until password is changed
                            changeReminder.Visible = true;
                        }
                    }
                }
            }
            else {
                Response.Redirect("Login.aspx?src=" + "timeout", false);
            }
        }

        protected void loadUser(string id) {
            SqlConnection connection = new SqlConnection(ConnectionString);
            string sql = "SELECT * FROM Account WHERE Id=@USER";
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@USER", id);
            try {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader["Id"] != DBNull.Value) { lb_email.Text = reader["Id"].ToString(); }
                        if (reader["firstName"] != DBNull.Value && reader["lastName"] != DBNull.Value) { lb_name.Text = reader["firstName"].ToString() + " " + reader["lastName"].ToString(); }
                        if (reader["dateOfBirth"] != DBNull.Value) { lb_dob.Text = reader["dateOfBirth"].ToString(); }
                        if (reader["creditPIN"] != DBNull.Value) { cc = Convert.FromBase64String(reader["creditPIN"].ToString()); }
                        if (reader["IV"] != DBNull.Value) { IV = Convert.FromBase64String(reader["IV"].ToString()); }
                        if (reader["Key"] != DBNull.Value) { Key = Convert.FromBase64String(reader["Key"].ToString()); }
                    }
                }
                DateTime userDOB = DateTime.ParseExact(lb_dob.Text, "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                lb_dob.Text = userDOB.Date.ToString("d/M/yyyy");
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }
        }

        protected string decryptData(byte[] cipherText) {
            string plainText = null;
            try {
                RijndaelManaged cipher = new RijndaelManaged();
                cipher.IV = IV;
                cipher.Key = Key;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptTransform = cipher.CreateDecryptor();
                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptTransform, CryptoStreamMode.Read)) {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt)) {
                            // Read the decrypted bytes from the decrypting stream and place them in a string.
                            plainText = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex) { throw new Exception(ex.ToString()); }
            finally { }
            return plainText;
        }

        protected void btn_logout_Click(object sender, EventArgs e) {
            Session.Clear();
            Session.Abandon();
            Session.RemoveAll();

            Response.Redirect("Login.aspx", false);

            if (Request.Cookies["ASP.NET_SessionId"] != null) {
                Response.Cookies["ASP.NET_SessionId"].Value = string.Empty;
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddMonths(-24);
            }

            if (Request.Cookies["AuthToken"] != null) {
                Response.Cookies["AuthToken"].Value = string.Empty;
                Response.Cookies["AuthToken"].Expires = DateTime.Now.AddMonths(-24);
            }
        }

        protected void btn_reveal_Click(object sender, EventArgs e) {
            // if currently hidden, verify session
            if (btn_reveal.Text == "Reveal") {
                if (Session["UserId"] != null && Session["AuthToken"] != null && Request.Cookies["AuthToken"] != null) {
                    if (!Session["AuthToken"].ToString().Equals(Request.Cookies["AuthToken"].Value)) {
                        Response.Redirect("Login.aspx?src=" + "timeout", false);
                    }
                    else {
                        lb_cc.Text = decryptData(cc);
                        btn_reveal.Text = "Hide";
                    }
                }
                else {
                    Response.Redirect("Login.aspx?src=" + "timeout", false);
                }
            }
            else if (btn_reveal.Text == "Hide") { // not sure why the reveal bool is always returning false
                string newStr = "";
                for (int i = 0; i < lb_cc.Text.Length; i++) {
                    if ((i+1) % 5 == 0) newStr += " ";
                    if (lb_cc.Text[i] != ' ') newStr += "*";
                }
                lb_cc.Text = newStr;
                btn_reveal.Text = "Reveal";
            }
        }

        public class CaptchaMessage {
            public string success { get; set; }
            public List<string> ErrorMessage { get; set; }
        }

        public bool ValidateCaptcha() {
            bool result = true;
            string captchaResponse = Request.Form["g-recaptcha-response"];

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://www.google.com/recaptcha/api/siteverify?secret=6LenlfwZAAAAAMGhTIiQgpCfjXjbWzHzl4LjUhB9 &response="
                + captchaResponse);
            try {
                using (WebResponse wResponse = req.GetResponse()) {
                    using (StreamReader readStream = new StreamReader(wResponse.GetResponseStream())) {
                        string jsonResponse = readStream.ReadToEnd();
                        JavaScriptSerializer js = new JavaScriptSerializer();

                        CaptchaMessage jsonObject = js.Deserialize<CaptchaMessage>(jsonResponse);
                        result = Convert.ToBoolean(jsonObject.success);
                    }
                }
                return result;
            }
            catch (WebException ex) {
                throw ex;
            }
        } // validateCaptcha

        private int validatePassword() {
            string pw = HttpUtility.HtmlEncode(tb_new.Text.Trim());
            int score = 0;
            // Score 1
            if (pw.Length < 8 || pw.Length > 32) { pwSuggestion += "New password should be more than 8 characters but less than 32. <br>"; return 1; }
            else score++;

            // Score 2
            if (!Regex.IsMatch(pw, "[a-z\t]")) { pwSuggestion += "Your new password is missing a small case alphabetical. <br>"; return score; }
            else score++;

            // Score 3
            if (!Regex.IsMatch(pw, "[A-Z\t]")) { pwSuggestion += "Your new password is missing a large case alphabetical. <br>"; return score; }
            else score++;

            // Score 4
            if (!Regex.IsMatch(pw, "[0-9\t]")) { pwSuggestion += "Your new password is missing a digit. <br>"; return score; }
            else score++;

            // Score 5, exclude characters ' and " to prevent SQL injection
            if (!Regex.IsMatch(pw, "[^a-zA-Z0-9\'\"]")) { pwSuggestion += "Your new password should use a special character that excludes quotation marks. <br>"; return score; }
            else score++;

            // score 6
            if (Regex.IsMatch(pw, @"([a-zA-Z0-9])\1{2,}")) { pwSuggestion += "Do not repeat a character more than twice in a row in your new password. <br>"; return score; }
            else score++;

            return score;
        } // validate password

        private bool validateOld() {
            string pwd = HttpUtility.HtmlEncode(tb_old.Text.Trim());
            // just a regex to make sure password length is correct and does not contain quotes
            if (pwd.Length < 8 || pwd.Length > 32 || Regex.IsMatch(pwd, "[\'\"]")) {
                pwSuggestion = "Old password does not match.";
                return false;
            }
            return true;
        }

        private int passwordAge(string id) {
            // returns password age in total minutes
            int age = 0;
            SqlConnection connection = new SqlConnection(ConnectionString);
            string sql = "SELECT passwordTime FROM Account WHERE Id=@INPUTID";
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@INPUTID", id);
            try {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader["passwordTime"] != null && reader["passwordTime"] != DBNull.Value) {
                            DateTime now = DateTime.Now;
                            DateTime pwTime = DateTime.Parse(reader["passwordTime"].ToString());
                            TimeSpan diff = now - pwTime;
                            age = Convert.ToInt32(diff.TotalMinutes);
                        }
                    }
                }
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }
            return age;
        }

        protected string getDBHash(string id, string column) {
            string h = null;
            SqlConnection connection = new SqlConnection(ConnectionString);
            // dynamic sql to parameterise column name
            string sql = string.Format("SELECT {0} FROM Account WHERE Id=@INPUTID", column);
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@INPUTID", id);
            try {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader[column] != null && reader[column] != DBNull.Value) {
                            h = reader[column].ToString();
                        }
                        else {
                            return h;
                        }
                    }
                }
            }
            catch (Exception) {
                lb_warning.Text = "Fatal error occurred.";
            }
            finally { connection.Close(); }
            return h;
        } // obtain password hash

        protected string getDBSalt(string id, string column) {
            string s = null;
            SqlConnection connection = new SqlConnection(ConnectionString);
            string sql = string.Format("SELECT {0} FROM Account WHERE Id=@INPUTID", column);
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@INPUTID", id);
            try {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader[column] != null && reader[column] != DBNull.Value) {
                            s = reader[column].ToString();
                        }
                        else {
                            return s;
                        }
                    }
                }
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }
            return s;
        }

        private bool comparePw(string id) {
            bool success = false;
            string curPWInput = tb_old.Text.Trim().ToString();
            string newPWInput = tb_new.Text.Trim().ToString();
            if (curPWInput == newPWInput) { return success; } // password input is the same, return false by default
            SHA512Managed Current_hash = new SHA512Managed();
            string Current_dbHash = getDBHash(id, "PasswordHash");
            string dbSalt = getDBSalt(id, "PasswordSalt");
            if (dbSalt != null && dbSalt.Length > 0 && Current_dbHash != null && Current_dbHash.Length > 0) {
                string pwdWithSalt = curPWInput + dbSalt;
                byte[] hashWithSalt = Current_hash.ComputeHash(Encoding.UTF8.GetBytes(pwdWithSalt));
                string userHash = Convert.ToBase64String(hashWithSalt);
                if (userHash.Equals(Current_dbHash)) {
                    // if password is correct, then check older passwords
                    // check the more recent one first
                    string newPwSalted = newPWInput + dbSalt;
                    SHA512Managed Old_hash = new SHA512Managed();
                    string Old_dbHash = getDBHash(id, "firstOldPwHash");
                    if (Old_dbHash != null && Old_dbHash.Length > 0) {
                        byte[] old_HashWithSalt = Old_hash.ComputeHash(Encoding.UTF8.GetBytes(newPwSalted));
                        string oldPwHash = Convert.ToBase64String(old_HashWithSalt);
                        if (oldPwHash.Equals(Old_dbHash)) return success;

                        // check the older password
                        SHA512Managed Older_hash = new SHA512Managed();
                        string Older_dbHash = getDBHash(id, "secondOldPwHash");
                        if (Older_dbHash != null && Older_dbHash.Length > 0) {
                            byte[] older_HashWithSalt = Older_hash.ComputeHash(Encoding.UTF8.GetBytes(newPwSalted));
                            string olderPwHash = Convert.ToBase64String(older_HashWithSalt);
                            if (olderPwHash.Equals(Older_dbHash)) return success;
                            else { success = true; } // password matches with neither old password
                        }
                        else { success = true; } // there is no older password to check
                    }
                    else { success = true; } // there are no old passwords, so it's ok to use any password
                }
                else return success; // current password doesn't match, return false as default
            }
            return success;
        }

        private void updateDBPW(string id) {
            string currPw = null;
            string oldPw = null;
            SqlConnection connection = new SqlConnection(ConnectionString);
            // obtain current and first old passwords
            string retrieve_sql = "SELECT passwordHash, firstOldPwHash FROM Account WHERE Id=@INPUTID";
            SqlCommand retrieve_command = new SqlCommand(retrieve_sql, connection);
            retrieve_command.Parameters.AddWithValue("@INPUTID", id);
            try {
                connection.Open();
                using (SqlDataReader reader = retrieve_command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader["passwordHash"] != null && reader["passwordHash"] != DBNull.Value) { currPw = reader["passwordHash"].ToString(); }
                        if (reader["firstOldPwHash"] != null && reader["firstOldPwHash"] != DBNull.Value) { oldPw = reader["firstOldPwHash"].ToString(); }
                    }
                }
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }

            // update: password age = now | first old password = current password | second old password = first old password
            // current password = input password

            string update_sql = "UPDATE Account SET passwordHash=@PH, passwordTime=@PT, firstOldPwHash=@FOPH, secondOldPwHash=@SOPH WHERE Id=@INPUTID";
            SqlCommand update_command = new SqlCommand(update_sql, connection);
            DateTime now = DateTime.Now;
            update_command.Parameters.AddWithValue("@INPUTID", id);
            update_command.Parameters.AddWithValue("@PH", finalHash);
            update_command.Parameters.AddWithValue("@PT", now);
            update_command.Parameters.AddWithValue("@FOPH", currPw);
            // check if the first old password is null
            if (oldPw != null) { update_command.Parameters.AddWithValue("@SOPH", oldPw); }
            else { update_command.Parameters.AddWithValue("@SOPH", DBNull.Value); }
            try {
                connection.Open();
                update_command.ExecuteNonQuery();
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }
        }

        public void passwordHash() {
            string basePW = tb_new.Text.ToString().Trim();

            string salt = getDBSalt((string)Session["UserId"], "PasswordSalt");
            string pwdWithSalt = basePW + salt;

            SHA512Managed hashing = new SHA512Managed();
            byte[] plainHash = hashing.ComputeHash(Encoding.UTF8.GetBytes(basePW));
            byte[] hashWithSalt = hashing.ComputeHash(Encoding.UTF8.GetBytes(pwdWithSalt));
            finalHash = Convert.ToBase64String(hashWithSalt);
        } // password hash

        protected void btn_password_Click(object sender, EventArgs e) {
            if (Session["UserId"] != null && Session["AuthToken"] != null && Request.Cookies["AuthToken"] != null) {
                if (!Session["AuthToken"].ToString().Equals(Request.Cookies["AuthToken"].Value)) {
                    Response.Redirect("Login.aspx?src=" + "expire", false);
                }
                else if (!ValidateCaptcha()) {
                    Response.Redirect("Login.aspx?src=" + "expire", false);
                }
                else {
                    if (validatePassword() == 6 && validateOld()) {
                        if (passwordAge((string)Session["UserId"]) > 5) {
                            if (comparePw((string)Session["UserId"])) {
                                passwordHash();
                                updateDBPW((string)Session["UserId"]);
                                Response.Redirect("UserProfile.aspx?src=" + "update");
                            }
                            else {
                                lb_warning.Text = "You failed the password check. Please try again.";
                                lb_warning.ForeColor = Color.Red;
                            }
                        }
                        else {
                            lb_warning.Text = "Your new password is too recent. Please wait before changing it again.";
                            lb_warning.ForeColor = Color.Red;
                        }
                    }
                    else {
                        lb_warning.Text = pwSuggestion;
                        lb_warning.ForeColor = Color.Red;
                    }
                }
            }
        }
    } // page class
} // namespace