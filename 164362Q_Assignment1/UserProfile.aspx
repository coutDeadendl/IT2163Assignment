<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UserProfile.aspx.cs" Inherits="_164362Q_Assignment1.UserProfile" %>

<!DOCTYPE html>

<!-- TODO: password change, session timeout,
    error pages, advanced features -->

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>SITConnect</title>
    <script src="https://www.google.com/recaptcha/api.js?render=6LenlfwZAAAAAErbOxpZHMSCOUHSgoUTYUdqWnWc"></script>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@4.5.3/dist/css/bootstrap.min.css" integrity="sha384-TX8t27EcRE3e/ihU7zmQxVncDAy5uIKz4rEkgIXeMed4M0jlfIDPvg6uqKI2xXr2" crossorigin="anonymous"/>    
        <script type="text/javascript">
        function validate() {
            var str = document.getElementById('<%=tb_new.ClientID %>').value;
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
                document.getElementById("btn_submit").disabled = false;
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
    <div class="mx-auto" style="width:95%;">
        <center><p class="h1">SITConnect!</p></center>
        <asp:Panel ID="updateSuccess" runat="server" visible="false">
            <div class="alert alert-success m-auto p-3" style="width:90%;" role="alert">Password has been updated!</div>
        </asp:Panel>
        <asp:Panel ID="changeReminder" runat="server" visible="false">
            <div class="alert alert-warning m-auto p-3" style="width:90%;" role="alert">Please change your password!</div>
        </asp:Panel>
        <form id="form1" runat="server">
            <p class="h2">User Profile</p>
            <hr />
            <p class="mb-0 font-weight-bold">Name:</p>
            <asp:Label ID="lb_name" class="mb-2" runat="server" Text="Name"></asp:Label>
            <p class="mt-3 mb-0 font-weight-bold">E-mail Address:</p>
            <asp:Label ID="lb_email" class="mb-2" runat="server" Text="Email"></asp:Label>
            <p class="mt-3 mb-0 font-weight-bold">Date of Birth:</p>
            <asp:Label ID="lb_dob" class="mb-2" runat="server" Text="DOB"></asp:Label>
            <p class="mt-3 mb-0 font-weight-bold">Credit Card PIN:</p>
            <table cellspacing="3"><tr>
                <td><asp:Label ID="lb_cc" class="mb-2" runat="server" Text="**** **** **** ****"></asp:Label></td>
                <td><asp:Button ID="btn_reveal" runat="server" class="btn btn-outline-dark" Text="Reveal" OnClick="btn_reveal_Click" /></td>
            </tr></table>
            <hr />
            <p class="h3">Change password:</p>
            <table cellspacing="3">
                <tr>
                    <td><p class="font-weight-bold">Old password: &nbsp;</p></td>
                    <td><asp:TextBox ID="tb_old" runat="server" TextMode="Password"></asp:TextBox></td>
                </tr>
                <tr>
                    <td><p class="font-weight-bold">New password: &nbsp;</p></td>
                    <td><asp:TextBox ID="tb_new" TextMode="Password" onkeypress="validate();" runat="server"></asp:TextBox></td>
                </tr>
                <tr>
                    <td><asp:Button ID="btn_submit" runat="server" class="btn btn-outline-dark" Text="Change Password" OnClick="btn_password_Click" /> </td>
                    <td><input type="hidden" id="g-recaptcha-response" name="g-recaptcha-response"/>
                        <asp:Label ID="lb_warning" runat="server"></asp:Label></td>
                </tr>
            </table>
            <center><asp:Button ID="btn_logout" runat="server" class="btn btn-outline-dark" Text="Logout" OnClick="btn_logout_Click" /> </center>
        </form>
    </div>
</body>
</html>