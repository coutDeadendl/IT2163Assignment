<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Registration.aspx.cs" Inherits="_164362Q_Assignment1.Registration" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>SITConnect - Registration</title>
    <script src="https://www.google.com/recaptcha/api.js?render=6LenlfwZAAAAAErbOxpZHMSCOUHSgoUTYUdqWnWc"></script>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@4.5.3/dist/css/bootstrap.min.css" integrity="sha384-TX8t27EcRE3e/ihU7zmQxVncDAy5uIKz4rEkgIXeMed4M0jlfIDPvg6uqKI2xXr2" crossorigin="anonymous"/>    
    <script type="text/javascript">
        function validate() {
            var str = document.getElementById('<%=tb_password.ClientID %>').value;
            if (str.length < 8) {
                document.getElementById("lb_warning").style.color = 'red';
                document.getElementById("lb_warning").innerHTML = "Password length must be less than 8."
            }
            else if (str.search(/[0-9]/) == -1) {
                document.getElementById("lb_warning").style.color = 'red';
                document.getElementById("lb_warning").innerHTML = "Password requires numerical."
            }
            else if (str.search(/[a-z\t]/) == -1) {
                document.getElementById("lb_warning").style.color = 'red';
                document.getElementById("lb_warning").innerHTML = "Password requires at least one small alphabetical."
            }
            else if (str.search(/[A-Z\t]/) == -1) {
                document.getElementById("lb_warning").style.color = 'red';
                document.getElementById("lb_warning").innerHTML = "Password requires at least one big alphabetical."
            }
            else if (str.search(/[^a-zA-Z0-9\'\"]/) == -1) {
                document.getElementById("lb_warning").style.color = 'red';
                document.getElementById("lb_warning").innerHTML = "Password requires special characters."
            }
            else if (str.search(/([a-zA-Z0-9])\1{2,}/) != -1) {
                document.getElementById("lb_warning").style.color = 'red';
                document.getElementById("lb_warning").innerHTML = "Password must not have more than 2 repeating characters in a row."
            }
            else {
                document.getElementById("lb_warning").innerHTML = ""
                document.getElementById("btn_signup").disabled = false;
            }
        }
    </script>
</head>
<body>
    <script>
        grecaptcha.ready(function () {
            grecaptcha.execute('6LenlfwZAAAAAErbOxpZHMSCOUHSgoUTYUdqWnWc', { action: 'Register' }).then(function (token) {
                document.getElementById("g-recaptcha-response").value = token;
            });
        });
    </script>
    <div>
        <center><p class="h1">SITConnect!</p></center>
        <form id="form1" runat="server">
            <fieldset>
                <legend class="font-weight-bold">Registration</legend>
                <table cellpadding="3">
                    <tr>
                        <td><asp:Label ID="lb_Fname" runat="server" Text="First Name"></asp:Label></td>
                        <td><asp:TextBox ID="tb_Fname" runat="server" required></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td><asp:Label ID="lb_Lname" runat="server" Text="Last Name"></asp:Label></td>
                        <td><asp:TextBox ID="tb_Lname" runat="server" required></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td><asp:Label ID="lb_card" runat="server" Text="Credit Card PIN"></asp:Label></td>
                        <td><asp:TextBox ID="tb_card" runat="server" required></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td><asp:Label ID="lb_email" runat="server" Text="Email Address"></asp:Label></td>
                        <td><asp:TextBox ID="tb_email" runat="server" required></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td><asp:Label ID="lb_password" runat="server" Text="Password"></asp:Label></td>
                        <td><asp:TextBox ID="tb_password" runat="server" TextMode="Password" onkeypress="validate();" required></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td><asp:Label ID="lb_dob" runat="server" Text="Date of Birth"></asp:Label></td>
                        <td><asp:TextBox ID="tb_dob" runat="server" TextMode="Date" required></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td><asp:Button ID="btn_signup" runat="server" class="btn btn-outline-dark" Text="Sign Up" OnClick="btn_signup_Click" />
                        </td>
                        <td>
                            <input type="hidden" id="g-recaptcha-response" name="g-recaptcha-response"/>
                            <asp:Label ID="lb_warning" runat="server"></asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <td><a href="/Login.aspx">Login instead?</a></td>
                    </tr>
                </table>
            </fieldset>
        </form>
    </div>
</body>
</html>