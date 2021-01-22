using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;

/* MY DEMO REGISTRATION DETAILS
 * Email: mysample@sample.com
 * Password: !S@MPL3M3ok
 */

/* PASSWORD CHANGE DEMO
 * Email: thisis@amail.com
 * Password: P@55Word!
 * New Password: P@SSw0rd!
 * New-New Password: P!55w0rd!
 */

namespace _164362Q_Assignment1 {
    public partial class Registration : System.Web.UI.Page {
        string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SITConnectDB"].ConnectionString;
        static string finalHash;
        static string salt;
        byte[] Key;
        byte[] IV;
        string pwSuggestion = "";
        protected void Page_Load(object sender, EventArgs e) {
            // signup button disabled until password passes client check.
            btn_signup.Enabled = false;
        }

        protected void btn_signup_Click(object sender, EventArgs e) {
            // validate input on server-side
            if (validatePassword() == 6 && validateInput() == 4) {
                // then captcha check next
                if (ValidateCaptcha()) {
                    // check if id exists
                    using (SqlConnection con = new SqlConnection(ConnectionString)) {
                        SqlCommand cmd = new SqlCommand("SELECT * FROM Account WHERE Id=@email");
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@email", HttpUtility.HtmlEncode(tb_email.Text.Trim()));
                        cmd.Connection = con;
                        con.Open();
                        SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.HasRows) {
                            con.Close();
                            lb_warning.Text = "This e-mail cannot be used.";
                            lb_warning.ForeColor = Color.Red;
                        }
                        else {
                            con.Close();
                            // once captcha and e-mail are fine, we begin the actual registration
                            passwordHash();
                            // create user codes is inside passwordHash function
                            // when registration is done, bring user to login page with success message
                            Response.Redirect("Login.aspx?src=" + "registration");
                        } // if reader has rows
                    } // using Sql Connection
                } // captcha check
                else {
                    lb_warning.Text = "Failed Captcha check";
                    lb_warning.ForeColor = Color.Red;
                } // if validatecaptcha() failed
            } // password check
            else {
                lb_warning.Text = pwSuggestion;
                lb_warning.ForeColor = Color.Red;
            } // if password check failed
        } // btnsignup_click

        public class CaptchaMessage{
            public string success { get; set; }
            public List<string> ErrorMessage { get; set; }
        } // captcha

        public bool ValidateCaptcha() {
            bool result = true;
            string captchaResponse = Request.Form["g-recaptcha-response"];

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://www.google.com/recaptcha/api/siteverify?secret=6LenlfwZAAAAAMGhTIiQgpCfjXjbWzHzl4LjUhB9 &response="
                + captchaResponse);
            try {
                using (WebResponse wResponse = req.GetResponse()) {
                    using (StreamReader readStream = new StreamReader(wResponse.GetResponseStream())) {
                        string jsonResponse = readStream.ReadToEnd();
                        lb_warning.Text = jsonResponse.ToString();
                        JavaScriptSerializer js = new JavaScriptSerializer();

                        CaptchaMessage jsonObject = js.Deserialize<CaptchaMessage>(jsonResponse);
                        result = Convert.ToBoolean(jsonObject.success);
                    }
                }
                return result;
            }
            catch (WebException) {
                lb_warning.Text = "Fatal error occurred.";
                return false;
            }
        } // validateCaptcha

        private int validatePassword(){
            string pw = HttpUtility.HtmlEncode(tb_password.Text.Trim());
            int score = 0;
            // Score 1
            if (pw.Length < 8 || pw.Length > 32) { pwSuggestion += "Password should be more than 8 characters but less than 32. <br>"; return 1; }
            else score++;

            // Score 2
            if (!Regex.IsMatch(pw, "[a-z\t]")) { pwSuggestion += "Your password is missing a small case alphabetical. <br>"; return score; }
            else score++;

            // Score 3
            if (!Regex.IsMatch(pw, "[A-Z\t]")) { pwSuggestion += "Your password is missing a large case alphabetical. <br>"; return score; }
            else score++;

            // Score 4
            if (!Regex.IsMatch(pw, "[0-9\t]")) { pwSuggestion += "Your password is missing a digit. <br>"; return score; }
            else score++;

            // Score 5, exclude characters ' and " to prevent SQL injection
            if (!Regex.IsMatch(pw, "[^a-zA-Z0-9\'\"]")) { pwSuggestion += "Use a special character in your password that excludes quotation marks. <br>"; return score; }
            else score++;

            // score 6
            if (Regex.IsMatch(pw, @"([a-zA-Z0-9])\1{2,}")) { pwSuggestion += "Do not repeat a character more than twice in a row in your password. <br>"; return score; }
            else score++;

            return score;
        } // validate password

        private int validateInput() {
            int score = 0;

            // check user age, score 1
            DateTime currentyear = DateTime.Now;
            DateTime userDOB = DateTime.ParseExact(tb_dob.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (currentyear.Year - userDOB.Year < 13) { pwSuggestion += "You must be at least 13 years old to register with SITConnect. <br>"; return 1; }
            else score++;

            // check input e-mail, score 2 (prevent XSS)
            if (!emailChecker()) { pwSuggestion += "Use a valid e-mail address. <br>"; return score; }
            else score++;

            // check if credit card PIN is exactly 16 digits, score 3 (prevent XSS)
            // users may input PIN with spaces, so Regex is used to remove all spaces.
            string userPIN = HttpUtility.HtmlEncode(Regex.Replace(tb_card.Text, @"\s+", ""));
            if (!Regex.IsMatch(userPIN, "([0-9]{16})+")) { pwSuggestion += "Use a valid credit card PIN (16 digits). <br>"; return score; }
            else score++;

            // check that first name and last name do not have special characters, score 4 (prevent XSS)
            string firstname = HttpUtility.HtmlEncode(tb_Fname.Text.Trim());
            string lastname = HttpUtility.HtmlEncode(tb_Lname.Text.Trim());
            // names will allow: All alphabetical, as well as ' and - for special names
            if (Regex.IsMatch(firstname, @"[^A-Za-z-']") || Regex.IsMatch(lastname, @"[^A-Za-z-']")) { pwSuggestion += "Only alphanumerical and the symbols ' and - are allowed in names. <br>"; return score; }
            else score++;

            return score;
        }

        public bool emailChecker() {
            string useremail = HttpUtility.HtmlEncode(tb_email.Text.Trim());
            try {
                MailAddress m = new MailAddress(useremail);
                return true;
            }
            catch (FormatException) {
                return false;
            }
        }

        public void newAccount(){
            using (SqlConnection con = new SqlConnection(ConnectionString)) {
                using (SqlCommand cmd = new SqlCommand("INSERT INTO Account VALUES(@Id, @FirstName, @LastName, @CreditPIN, @PasswordHash, @PasswordSalt, @DateOfBirth, @IV, @Key, @PasswordTime, @LoginAttempts, @LoginFailTime, @OldHash, @OlderHash)")) {
                    using (SqlDataAdapter sda = new SqlDataAdapter()) {
                        // encode all non-encrypted/hashed textbox values
                        cmd.CommandType = CommandType.Text;
                        DateTime now = DateTime.Now;
                        cmd.Parameters.AddWithValue("@Id", tb_email.Text.Trim());
                        cmd.Parameters.AddWithValue("@FirstName", tb_Fname.Text.Trim());
                        cmd.Parameters.AddWithValue("@LastName", tb_Lname.Text.Trim());
                        cmd.Parameters.AddWithValue("@CreditPIN", Convert.ToBase64String(encryptData(tb_card.Text.Trim())));
                        cmd.Parameters.AddWithValue("@PasswordHash", finalHash);
                        cmd.Parameters.AddWithValue("@PasswordSalt", salt);
                        cmd.Parameters.AddWithValue("@DateOfBirth", DateTime.ParseExact(tb_dob.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture));
                        cmd.Parameters.AddWithValue("@IV", Convert.ToBase64String(IV));
                        cmd.Parameters.AddWithValue("@Key", Convert.ToBase64String(Key));
                        cmd.Parameters.AddWithValue("@PasswordTime", now);
                        // the compiler complains if I don't add these for some reason
                        cmd.Parameters.AddWithValue("@LoginAttempts", 0);
                        cmd.Parameters.AddWithValue("@LoginFailTime", DBNull.Value);
                        cmd.Parameters.AddWithValue("@OldHash", DBNull.Value);
                        cmd.Parameters.AddWithValue("@OlderHash", DBNull.Value);
                        cmd.Connection = con;
                        try {
                            con.Open();
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception) { lb_warning.Text = "Fatal error occurred"; }
                        finally { con.Close(); }
                    }
                }
            }
        } // create new account

        public void passwordHash() {
            string basePW = tb_password.Text.ToString().Trim();

            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] saltByte = new byte[8];

            rng.GetBytes(saltByte);
            salt = Convert.ToBase64String(saltByte);
            string pwdWithSalt = basePW + salt;

            SHA512Managed hashing = new SHA512Managed();
            byte[] plainHash = hashing.ComputeHash(Encoding.UTF8.GetBytes(basePW));
            byte[] hashWithSalt = hashing.ComputeHash(Encoding.UTF8.GetBytes(pwdWithSalt));
            finalHash = Convert.ToBase64String(hashWithSalt);

            RijndaelManaged cipher = new RijndaelManaged();
            cipher.GenerateKey();
            Key = cipher.Key;
            IV = cipher.IV;
            // create new account right after hashing the password, to be safe
            newAccount();
        } // password hash

        protected byte[] encryptData(string data) {
            byte[] cipherText = null;
            try {
                RijndaelManaged cipher = new RijndaelManaged();
                cipher.IV = IV;
                cipher.Key = Key;
                ICryptoTransform encryptTransform = cipher.CreateEncryptor();
                byte[] plainText = Encoding.UTF8.GetBytes(data);
                cipherText = encryptTransform.TransformFinalBlock(plainText, 0, plainText.Length);
            }
            catch (Exception) { lb_warning.Text = "Fatal error occurred"; }
            finally { }
            return cipherText;
        } // encrypt data
    } // class
} // namespace