using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace _164362Q_Assignment1 {
    public partial class Login : System.Web.UI.Page {
        string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SITConnectDB"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e){
            // empty session since I keep force closing the browser
            EmptySession();
            // if user came from registration page, a success will pop up
            if (Request.QueryString["src"] == "registration") {
                regSuccess.Visible = true;
            }
            else if (Request.QueryString["src"] == "expire") {
                failure.Visible = true;
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
            catch (WebException) {
                lb_warning.Text = "Fatal error occurred."; return false;
            }
        } // validateCaptcha

        protected void btn_login_Click(object sender, EventArgs e) {
            // captcha check
            if (ValidateCaptcha()) {
                // sanitise user inputs
                string email = HttpUtility.HtmlEncode(tb_email.Text.ToString().Trim());
                string pwd = HttpUtility.HtmlEncode(tb_password.Text.ToString().Trim());
                // to make sure nothing goes wrong, validate the e-mail and password.
                if (emailChecker(email) && pwdChecker(pwd)) {
                        SHA512Managed hash = new SHA512Managed();
                        string dbHash = getDBHash(tb_email.Text.ToString().Trim());
                        string dbSalt = getDBSalt(tb_email.Text.ToString().Trim());
                    try {
                        // lockout check
                        if (LockoutAttempts(tb_email.Text.ToString().Trim()) < 3 || LockoutDate(tb_email.Text.ToString().Trim()) > 30) {
                            if (LockoutDate(tb_email.Text.ToString().Trim()) > 30) {
                                resetLogin(tb_email.Text.ToString().Trim());
                            }
                            // check again if it's more than 30 mins, if true then reset else do nothing
                            if (dbSalt != null && dbSalt.Length > 0 && dbHash != null && dbHash.Length > 0) {
                                string pwdWithSalt = pwd + dbSalt;
                                byte[] hashWithSalt = hash.ComputeHash(Encoding.UTF8.GetBytes(pwdWithSalt));
                                string userHash = Convert.ToBase64String(hashWithSalt);
                                if (userHash.Equals(dbHash)) {
                                    // handle session fixation
                                    EmptySession();
                                    // reset lockout
                                    resetLogin(tb_email.Text.ToString().Trim());
                                    // create new fresh session
                                    Session["UserId"] = tb_email.Text.ToString().Trim();
                                    string guid = Guid.NewGuid().ToString();
                                    Session["AuthToken"] = guid;
                                    Response.Cookies.Add(new HttpCookie("AuthToken", guid));
                                    Response.Redirect("UserProfile.aspx", false);
                                }
                                else {
                                    // fail password comparison
                                    addLoginFailure(tb_email.Text.ToString().Trim());
                                    lb_warning.Visible = true;
                                    lb_warning.Text = "Email Address or Password is incorrect. Please try again.";
                                    lb_warning.ForeColor = Color.Red;
                                }
                            }
                            else {
                                // account doesn't exist
                                lb_warning.Visible = true;
                                lb_warning.Text = "Email Address or Password is incorrect. Please try again.";
                                lb_warning.ForeColor = Color.Red;
                            }
                        }
                        else {
                            // fail lockout check
                            lb_warning.Visible = true;
                            lb_warning.Text = "Email Address or Password is incorrect. Please try again.";
                            lb_warning.ForeColor = Color.Red;
                        }
                    }
                    catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
                    finally { }
                }
                else {
                    lb_warning.Visible = true;
                    lb_warning.Text = "Email Address or Password is incorrect. Please try again.";
                    lb_warning.ForeColor = Color.Red;
                }
            }
            else {
                lb_warning.Visible = true;
                lb_warning.Text = "Failed Captcha check";
                lb_warning.ForeColor = Color.Red;
            }
        }

        public bool emailChecker(string email) {
            try {
                MailAddress m = new MailAddress(email);
                return true;
            }
            catch (Exception ex) {
                if (ex is FormatException || ex is ArgumentNullException) {
                    return false;
                }
                return false;
            }
        }

        public bool pwdChecker(string pwd) {
            // just a regex to make sure password length is correct and does not contain quotes
            if (pwd.Length < 8 || pwd.Length > 32 || Regex.IsMatch(pwd, "[\'\"]")) {
                return false;
            }
            return true;
        }

        protected string getDBHash(string id) {
            string h = null;
            SqlConnection connection = new SqlConnection(ConnectionString);
            string sql = "SELECT PasswordHash FROM Account WHERE Id=@INPUTID";
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@INPUTID", id);
            try {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader["PasswordHash"] != null) {
                            if (reader["PasswordHash"] != DBNull.Value) {
                                h = reader["PasswordHash"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }
            return h;
        } // obtain password hash

        protected string getDBSalt(string id) {
            string s = null;
            SqlConnection connection = new SqlConnection(ConnectionString);
            string sql = "SELECT PasswordSalt FROM Account WHERE Id=@INPUTID";
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@INPUTID", id);
            try {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader["PasswordSalt"] != null) {
                            if (reader["PasswordSalt"] != DBNull.Value) {
                                s = reader["PasswordSalt"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }
            return s;
        }

        protected void EmptySession() {
            if (Request.Cookies["ASP.NET_SessionId"] != null) {
                Response.Cookies["ASP.NET_SessionId"].Value = string.Empty;
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddMonths(-64);
            }
            if (Request.Cookies["AuthToken"] != null) {
                Response.Cookies["AuthToken"].Value = string.Empty;
                Response.Cookies["AuthToken"].Expires = DateTime.Now.AddMonths(-64);
            }
        }

        protected void addLoginFailure(string id) {
            int oldCount = 0;
            SqlConnection connection = new SqlConnection(ConnectionString);
            string selectCount = "SELECT loginAttempts FROM Account WHERE Id=@INPUTID";
            SqlCommand countCmd = new SqlCommand(selectCount, connection);
            countCmd.Parameters.AddWithValue("@INPUTID", id);
            try {
                connection.Open();
                using (SqlDataReader reader = countCmd.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader["loginAttempts"] != null) {
                            if (reader["loginAttempts"] != DBNull.Value) {
                                oldCount = Convert.ToInt32(reader["loginAttempts"]);
                            }
                        }
                    }
                }
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }

            string sql = "UPDATE Account SET loginAttempts=@LA, loginFailTime=@LFT WHERE Id=@INPUTID";
            SqlCommand command = new SqlCommand(sql, connection);
            DateTime now = DateTime.Now;
            command.Parameters.AddWithValue("@INPUTID", id);
            command.Parameters.AddWithValue("@LFT", now);
            command.Parameters.AddWithValue("@LA", oldCount+1);
            try {
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }
        }

        protected void resetLogin(string id) {
            // open DB and reset properties
            SqlConnection connection = new SqlConnection(ConnectionString);
            string sql = "UPDATE Account SET loginAttempts=@LA, loginFailTime=@LFT WHERE Id=@INPUTID";
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@INPUTID", id);
            command.Parameters.AddWithValue("@LA", 0);
            command.Parameters.AddWithValue("@LFT", DBNull.Value);
            try {
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception) {
                lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }
        }

        protected int LockoutAttempts(string id) {
            int attempt = 0;
            SqlConnection connection = new SqlConnection(ConnectionString);
            string sql = "SELECT loginAttempts FROM Account WHERE Id=@INPUTID";
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@INPUTID", id);
            // open DB and return properties
            try {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader["loginAttempts"] != null) {
                            if (reader["loginAttempts"] != DBNull.Value) {
                                attempt = Convert.ToInt32(reader["loginAttempts"]);
                            }
                        }
                    }
                }
            }
            catch (Exception) {
                lb_warning.Text = "Fatal error occurred.";
            }
            finally { connection.Close(); }
            // if attempt not successful, will return 0
            return attempt;
        }

        protected int LockoutDate(string id) {
            // return properties for time now - last lockout date in minutes?
            int lt = 0;
            SqlConnection connection = new SqlConnection(ConnectionString);
            string sql = "SELECT loginFailTime FROM Account WHERE Id=@INPUTID";
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@INPUTID", id);
            // open DB and return properties
            try {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        if (reader["loginFailTime"] != null) {
                            if (reader["loginFailTime"] != DBNull.Value) {
                                DateTime now = DateTime.Now;
                                DateTime registered = DateTime.Parse(reader["loginFailTime"].ToString());
                                TimeSpan diff = now - registered;
                                lt = Convert.ToInt32(diff.TotalMinutes);
                                // create two time values with loginFailTime and Now()
                                // minus loginFailTime from Now, must use timeSpan
                                // set lt to difference in total minutes to int
                            }
                        }
                    }
                }
            }
            catch (Exception) {
                lb_warning.Text = "Fatal error occurred."; }
            finally { connection.Close(); }
            // if try fails, return 0 minutes
            return lt;
        }
    } // page class
} // namespace